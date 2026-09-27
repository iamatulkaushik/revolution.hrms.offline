Option Strict On
Option Explicit On

Imports System
Imports Microsoft.Data.SqlClient
Imports HRMS.Common
Imports HRMS.Models

''' <summary>First-run checks made with a candidate connection string (before it is saved).</summary>
Public NotInheritable Class SetupRepo
    Private Sub New()
    End Sub

    Public Shared Function Probe(connectionString As String) As ConnectionProbe
        Dim result As New ConnectionProbe()
        Try
            Using cn As New SqlConnection(connectionString)
                cn.Open()
                result.ServerVersion = cn.ServerVersion
                Using cmd As New SqlCommand("sec.usp_Bootstrap_Needed", cn)
                    cmd.CommandType = System.Data.CommandType.StoredProcedure
                    result.AdminNeeded = Convert.ToInt32(cmd.ExecuteScalar()) = 1
                End Using
            End Using
        Catch ex As SqlException
            Throw Db.Translate(ex)
        Catch ex As ArgumentException
            Throw New BusinessException("The connection settings are not valid.", ex)
        End Try
        Return result
    End Function
End Class
