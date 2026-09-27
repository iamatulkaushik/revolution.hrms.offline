Option Strict On
Option Explicit On

Imports System
Imports System.Security.Cryptography

''' <summary>PBKDF2-HMAC-SHA256. Hash and salt are stored in sec.[User]; iterations stored per user.</summary>
Public NotInheritable Class PasswordHasher
    Public Const DEFAULT_ITERATIONS As Integer = 310000
    Public Const SALT_BYTES As Integer = 32
    Public Const HASH_BYTES As Integer = 32

    Private Sub New()
    End Sub

    Public Shared Function NewSalt() As Byte()
        Dim salt(SALT_BYTES - 1) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(salt)
        End Using
        Return salt
    End Function

    Public Shared Function Hash(password As String, salt As Byte(), iterations As Integer) As Byte()
        If password Is Nothing Then Throw New ArgumentNullException("password")
        If salt Is Nothing OrElse salt.Length < 16 Then Throw New ArgumentException("Salt too short.")
        If iterations < 10000 Then Throw New ArgumentException("Too few iterations.")
        Using kdf As New Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)
            Return kdf.GetBytes(HASH_BYTES)
        End Using
    End Function

    Public Shared Function Verify(password As String, salt As Byte(), iterations As Integer, expectedHash As Byte()) As Boolean
        Dim actual As Byte() = Hash(password, salt, iterations)
        Return ByteUtil.ConstantTimeEquals(actual, expectedHash)
    End Function
End Class
