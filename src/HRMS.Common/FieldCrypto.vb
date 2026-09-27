Option Strict On
Option Explicit On

Imports System
Imports System.Text

''' <summary>Encrypts sensitive text fields (Aadhaar, PAN, bank a/c, UAN, ESI IP) for VARBINARY(256) columns.
''' Each purpose is bound into the MAC, so a PAN blob cannot be swapped into the Aadhaar column unnoticed.</summary>
Public NotInheritable Class FieldCrypto
    Public Const PURPOSE_AADHAAR As String = "Employee.Aadhaar"
    Public Const PURPOSE_PAN As String = "Employee.PAN"
    Public Const PURPOSE_BANK_ACC As String = "Employee.BankAcc"
    Public Const PURPOSE_UAN As String = "Employee.UAN"
    Public Const PURPOSE_ESI_IP As String = "Employee.EsiIP"

    Private Const VERSION_1 As Byte = 1
    Private Const MAX_PLAIN_CHARS As Integer = 100

    Private ReadOnly _encKey As Byte()
    Private ReadOnly _macKey As Byte()

    ''' <param name="masterKey">64 bytes: 32 encryption + 32 MAC.</param>
    Public Sub New(masterKey As Byte())
        If masterKey Is Nothing OrElse masterKey.Length <> KeyStore.MASTER_KEY_BYTES Then
            Throw New ArgumentException("Master key must be 64 bytes.")
        End If
        _encKey = New Byte(AuthCrypto.KEY_BYTES - 1) {}
        _macKey = New Byte(AuthCrypto.KEY_BYTES - 1) {}
        Buffer.BlockCopy(masterKey, 0, _encKey, 0, AuthCrypto.KEY_BYTES)
        Buffer.BlockCopy(masterKey, AuthCrypto.KEY_BYTES, _macKey, 0, AuthCrypto.KEY_BYTES)
    End Sub

    ''' <summary>Returns Nothing for empty input (meaning "no value / unchanged").</summary>
    Public Function EncryptText(purpose As String, value As String) As Byte()
        If String.IsNullOrWhiteSpace(value) Then Return Nothing
        Dim text As String = value.Trim()
        If text.Length > MAX_PLAIN_CHARS Then Throw New BusinessException("Value too long to encrypt.")
        Dim blob As Byte() = AuthCrypto.Encrypt(_encKey, _macKey, Encoding.UTF8.GetBytes(text), Encoding.UTF8.GetBytes(purpose))
        Return ByteUtil.Concat(New Byte() {VERSION_1}, blob)
    End Function

    Public Function DecryptText(purpose As String, cipher As Byte()) As String
        If cipher Is Nothing OrElse cipher.Length = 0 Then Return Nothing
        If cipher(0) <> VERSION_1 Then Throw New System.Security.Cryptography.CryptographicException("Unknown ciphertext version.")
        Dim blob(cipher.Length - 2) As Byte
        Buffer.BlockCopy(cipher, 1, blob, 0, blob.Length)
        Dim plain As Byte() = AuthCrypto.Decrypt(_encKey, _macKey, blob, Encoding.UTF8.GetBytes(purpose))
        Return Encoding.UTF8.GetString(plain)
    End Function
End Class
