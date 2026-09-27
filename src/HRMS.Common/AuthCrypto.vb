Option Strict On
Option Explicit On

Imports System
Imports System.Security.Cryptography

''' <summary>AES-256-CBC + HMAC-SHA256 (encrypt-then-MAC). Blob = IV(16) | ciphertext | tag(32).
''' The context bytes are authenticated but not stored (binds ciphertext to its purpose).</summary>
Public NotInheritable Class AuthCrypto
    Public Const KEY_BYTES As Integer = 32
    Private Const IV_BYTES As Integer = 16
    Private Const BLOCK_BYTES As Integer = 16
    Private Const TAG_BYTES As Integer = 32

    Private Sub New()
    End Sub

    Public Shared Function Encrypt(encKey As Byte(), macKey As Byte(), plain As Byte(), context As Byte()) As Byte()
        CheckKeys(encKey, macKey)
        If plain Is Nothing Then Throw New ArgumentNullException("plain")
        Using aes As Aes = Aes.Create()
            aes.KeySize = 256
            aes.Mode = CipherMode.CBC
            aes.Padding = PaddingMode.PKCS7
            aes.Key = encKey
            aes.GenerateIV()
            Dim iv As Byte() = aes.IV
            Dim ct As Byte()
            Using enc As ICryptoTransform = aes.CreateEncryptor()
                ct = enc.TransformFinalBlock(plain, 0, plain.Length)
            End Using
            Dim body As Byte() = ByteUtil.Concat(iv, ct)
            Return ByteUtil.Concat(body, ComputeTag(macKey, context, body))
        End Using
    End Function

    Public Shared Function Decrypt(encKey As Byte(), macKey As Byte(), blob As Byte(), context As Byte()) As Byte()
        CheckKeys(encKey, macKey)
        If blob Is Nothing OrElse blob.Length < IV_BYTES + BLOCK_BYTES + TAG_BYTES Then
            Throw New CryptographicException("Ciphertext too short.")
        End If
        Dim bodyLen As Integer = blob.Length - TAG_BYTES
        Dim body(bodyLen - 1) As Byte
        Dim tag(TAG_BYTES - 1) As Byte
        Buffer.BlockCopy(blob, 0, body, 0, bodyLen)
        Buffer.BlockCopy(blob, bodyLen, tag, 0, TAG_BYTES)
        If Not ByteUtil.ConstantTimeEquals(tag, ComputeTag(macKey, context, body)) Then
            Throw New CryptographicException("Integrity check failed.")
        End If
        Dim iv(IV_BYTES - 1) As Byte
        Buffer.BlockCopy(body, 0, iv, 0, IV_BYTES)
        Using aes As Aes = Aes.Create()
            aes.KeySize = 256
            aes.Mode = CipherMode.CBC
            aes.Padding = PaddingMode.PKCS7
            aes.Key = encKey
            aes.IV = iv
            Using dec As ICryptoTransform = aes.CreateDecryptor()
                Return dec.TransformFinalBlock(body, IV_BYTES, bodyLen - IV_BYTES)
            End Using
        End Using
    End Function

    Private Shared Function ComputeTag(macKey As Byte(), context As Byte(), body As Byte()) As Byte()
        Dim ctx As Byte() = If(context, New Byte() {})
        Using h As New HMACSHA256(macKey)
            Return h.ComputeHash(ByteUtil.Concat(BitConverter.GetBytes(ctx.Length), ctx, body))
        End Using
    End Function

    Private Shared Sub CheckKeys(encKey As Byte(), macKey As Byte())
        If encKey Is Nothing OrElse encKey.Length <> KEY_BYTES Then Throw New ArgumentException("Encryption key must be 32 bytes.")
        If macKey Is Nothing OrElse macKey.Length <> KEY_BYTES Then Throw New ArgumentException("MAC key must be 32 bytes.")
    End Sub
End Class
