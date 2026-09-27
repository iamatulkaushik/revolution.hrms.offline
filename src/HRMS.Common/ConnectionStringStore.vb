Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text

''' <summary>Connection string kept protected on disk (DPAPI). Env var HRMS_CONN overrides (dev only).</summary>
Public NotInheritable Class ConnectionStringStore
    Public Const ENV_VAR As String = "HRMS_CONN"

    Private Sub New()
    End Sub

    Private Shared Function FilePath() As String
        Return Path.Combine(AppPaths.ConfigDir, "conn.dat")
    End Function

    Public Shared Function Load(protector As IKeyProtector) As String
        Dim fromEnv As String = Environment.GetEnvironmentVariable(ENV_VAR)
        If Not String.IsNullOrWhiteSpace(fromEnv) Then Return fromEnv
        If Not File.Exists(FilePath()) Then Throw New BusinessException("Database is not configured on this PC.")
        Return Encoding.UTF8.GetString(protector.Unprotect(File.ReadAllBytes(FilePath())))
    End Function

    Public Shared Sub Save(protector As IKeyProtector, connectionString As String)
        If String.IsNullOrWhiteSpace(connectionString) Then Throw New ArgumentException("Connection string required.")
        Directory.CreateDirectory(AppPaths.ConfigDir)
        File.WriteAllBytes(FilePath(), protector.Protect(Encoding.UTF8.GetBytes(connectionString)))
    End Sub
End Class
