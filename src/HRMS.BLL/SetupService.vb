Option Strict On
Option Explicit On

Imports System
Imports Microsoft.Data.SqlClient
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>First-run configuration: connection string build/test/save. Keeps the UI away from the DAL.</summary>
Public NotInheritable Class SetupService
    Private Sub New()
    End Sub

    Public Shared Function BuildConnectionString(server As String, database As String, userId As String,
                                                 password As String, trustServerCertificate As Boolean) As String
        If String.IsNullOrWhiteSpace(server) Then Throw New BusinessException("Server is required.")
        If String.IsNullOrWhiteSpace(database) Then Throw New BusinessException("Database is required.")
        If String.IsNullOrWhiteSpace(userId) Then Throw New BusinessException("Login is required.")
        Dim b As New SqlConnectionStringBuilder()
        b.DataSource = server.Trim()
        b.InitialCatalog = database.Trim()
        b.UserID = userId.Trim()
        b.Password = If(password, String.Empty)
        b.Encrypt = True
        b.TrustServerCertificate = trustServerCertificate
        b.ApplicationName = "RevolutionHRMS"
        b.ConnectTimeout = 10
        Return b.ConnectionString
    End Function

    ''' <summary>Connects, checks the database scripts exist, and reports whether the first admin is still needed.</summary>
    Public Shared Function Probe(connectionString As String) As ConnectionProbe
        Return SetupRepo.Probe(connectionString)
    End Function

    ''' <summary>Uses the connection string for this run (not saved).</summary>
    Public Shared Sub Configure(connectionString As String)
        Db.Configure(connectionString)
    End Sub

    ''' <summary>Saves the connection string (DPAPI-protected) and uses it.</summary>
    Public Shared Sub Save(connectionString As String, protector As IKeyProtector)
        ConnectionStringStore.Save(protector, connectionString)
        Db.Configure(connectionString)
    End Sub

    ''' <summary>Server name for the status bar (never includes credentials).</summary>
    Public Shared Function ServerName(connectionString As String) As String
        Try
            Return New SqlConnectionStringBuilder(connectionString).DataSource
        Catch ex As ArgumentException
            Return "?"
        End Try
    End Function
End Class
