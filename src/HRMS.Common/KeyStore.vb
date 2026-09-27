Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

''' <summary>Holds the 64-byte field-encryption master key, wrapped at rest by an IKeyProtector.
''' LAN note: every client PC needs the SAME key. Create once on the first PC, ExportToFile with a
''' passphrase, ImportFromFile on the others. BACK UP the export file: losing the key = losing all
''' encrypted Aadhaar/PAN/bank data.</summary>
Public NotInheritable Class KeyStore
    Public Const MASTER_KEY_BYTES As Integer = 64
    Private Const MIN_PASSPHRASE As Integer = 12
    Private Shared ReadOnly MAGIC As Byte() = Encoding.ASCII.GetBytes("HRK1")
    Private Shared ReadOnly EXPORT_CONTEXT As Byte() = Encoding.ASCII.GetBytes("HRMS.KeyExport")
    Private Const SALT_BYTES As Integer = 16

    Private ReadOnly _protector As IKeyProtector
    Private ReadOnly _path As String

    Public Sub New(protector As IKeyProtector, keyFilePath As String)
        If protector Is Nothing Then Throw New ArgumentNullException("protector")
        If String.IsNullOrWhiteSpace(keyFilePath) Then Throw New ArgumentException("Key path required.")
        _protector = protector
        _path = keyFilePath
    End Sub

    Public Shared Function DefaultPath() As String
        Return Path.Combine(AppPaths.KeyDir, "field.key")
    End Function

    Public Function Exists() As Boolean
        Return File.Exists(_path)
    End Function

    Public Sub CreateNew()
        If Exists() Then Throw New BusinessException("An encryption key already exists on this PC.")
        Dim key(MASTER_KEY_BYTES - 1) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(key)
        End Using
        Try
            Save(key)
        Finally
            ByteUtil.Wipe(key)
        End Try
        Logger.Info("Field encryption key created.")
    End Sub

    Public Function Load() As Byte()
        If Not Exists() Then Throw New BusinessException("Encryption key not found. Create or import the key first.")
        Dim key As Byte() = _protector.Unprotect(File.ReadAllBytes(_path))
        If key Is Nothing OrElse key.Length <> MASTER_KEY_BYTES Then
            Throw New CryptographicException("Key file is invalid.")
        End If
        Return key
    End Function

    Public Sub ExportToFile(exportPath As String, passphrase As String)
        CheckPassphrase(passphrase)
        Dim key As Byte() = Load()
        Try
            Dim salt(SALT_BYTES - 1) As Byte
            Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
                rng.GetBytes(salt)
            End Using
            Dim iterations As Integer = PasswordHasher.DEFAULT_ITERATIONS
            Dim encKey As Byte() = Nothing
            Dim macKey As Byte() = Nothing
            DeriveWrapKeys(passphrase, salt, iterations, encKey, macKey)
            Dim blob As Byte() = AuthCrypto.Encrypt(encKey, macKey, key, EXPORT_CONTEXT)
            Dim data As Byte() = ByteUtil.Concat(MAGIC, BitConverter.GetBytes(iterations), salt, blob)
            File.WriteAllBytes(exportPath, data)
        Finally
            ByteUtil.Wipe(key)
        End Try
        Logger.Info("Field encryption key exported.")
    End Sub

    Public Sub ImportFromFile(importPath As String, passphrase As String)
        If Exists() Then Throw New BusinessException("An encryption key already exists on this PC.")
        CheckPassphrase(passphrase)
        Dim data As Byte() = File.ReadAllBytes(importPath)
        Dim headerLen As Integer = MAGIC.Length + 4 + SALT_BYTES
        If data.Length <= headerLen Then Throw New BusinessException("Key file is invalid.")
        For i As Integer = 0 To MAGIC.Length - 1
            If data(i) <> MAGIC(i) Then Throw New BusinessException("Key file is invalid.")
        Next
        Dim iterations As Integer = BitConverter.ToInt32(data, MAGIC.Length)
        If iterations < 100000 OrElse iterations > 10000000 Then Throw New BusinessException("Key file is invalid.")
        Dim salt(SALT_BYTES - 1) As Byte
        Buffer.BlockCopy(data, MAGIC.Length + 4, salt, 0, SALT_BYTES)
        Dim blob(data.Length - headerLen - 1) As Byte
        Buffer.BlockCopy(data, headerLen, blob, 0, blob.Length)

        Dim encKey As Byte() = Nothing
        Dim macKey As Byte() = Nothing
        DeriveWrapKeys(passphrase, salt, iterations, encKey, macKey)
        Dim key As Byte()
        Try
            key = AuthCrypto.Decrypt(encKey, macKey, blob, EXPORT_CONTEXT)
        Catch ex As CryptographicException
            Throw New BusinessException("Wrong passphrase or damaged key file.", ex)
        End Try
        Try
            If key.Length <> MASTER_KEY_BYTES Then Throw New BusinessException("Key file is invalid.")
            Save(key)
        Finally
            ByteUtil.Wipe(key)
        End Try
        Logger.Info("Field encryption key imported.")
    End Sub

    Private Sub Save(key As Byte())
        Directory.CreateDirectory(Path.GetDirectoryName(_path))
        File.WriteAllBytes(_path, _protector.Protect(key))
    End Sub

    Private Shared Sub CheckPassphrase(passphrase As String)
        If String.IsNullOrEmpty(passphrase) OrElse passphrase.Length < MIN_PASSPHRASE Then
            Throw New BusinessException("Passphrase must be at least " & MIN_PASSPHRASE & " characters.")
        End If
    End Sub

    Private Shared Sub DeriveWrapKeys(passphrase As String, salt As Byte(), iterations As Integer, ByRef encKey As Byte(), ByRef macKey As Byte())
        Using kdf As New Rfc2898DeriveBytes(passphrase, salt, iterations, HashAlgorithmName.SHA256)
            Dim material As Byte() = kdf.GetBytes(AuthCrypto.KEY_BYTES * 2)
            encKey = New Byte(AuthCrypto.KEY_BYTES - 1) {}
            macKey = New Byte(AuthCrypto.KEY_BYTES - 1) {}
            Buffer.BlockCopy(material, 0, encKey, 0, AuthCrypto.KEY_BYTES)
            Buffer.BlockCopy(material, AuthCrypto.KEY_BYTES, macKey, 0, AuthCrypto.KEY_BYTES)
            ByteUtil.Wipe(material)
        End Using
    End Sub
End Class
