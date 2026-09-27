Option Strict On
Option Explicit On

Imports System
Imports HRMS.Common
Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Connection + stored-procedure helpers. All access goes through procs (rules.md 2.1).
''' Session keys are read-only per connection, so every data call opens a connection and runs
''' sec.usp_Session_Set first (pool reset clears the context between uses).</summary>
Public NotInheritable Class Db
    Private Const COMMAND_TIMEOUT As Integer = 60
    Private Shared _connectionString As String

    Private Sub New()
    End Sub

    Public Shared Sub Configure(connectionString As String)
        If String.IsNullOrWhiteSpace(connectionString) Then Throw New ArgumentException("Connection string required.")
        _connectionString = connectionString
    End Sub

    ''' <summary>Connection without session (login, bootstrap, password change only).</summary>
    Public Shared Function OpenRaw() As SqlConnection
        If _connectionString Is Nothing Then Throw New BusinessException("Database is not configured.")
        Dim cn As New SqlConnection(_connectionString)
        Dim ok As Boolean = False
        Try
            Run(Sub() cn.Open())
            ok = True
        Finally
            If Not ok Then cn.Dispose()
        End Try
        Return cn
    End Function

    ''' <summary>Connection with UserID/CompanyID context applied for the current AppSession.</summary>
    Public Shared Function OpenSession() As SqlConnection
        Dim s As UserSession = AppSession.Require()
        Dim cn As SqlConnection = OpenRaw()
        Dim ok As Boolean = False
        Try
            Using cmd As SqlCommand = Build(cn, "sec.usp_Session_Set", P("@UserID", s.UserID), P("@CompanyID", s.CompanyID))
                Run(Sub() cmd.ExecuteNonQuery())
            End Using
            ok = True
        Finally
            If Not ok Then cn.Dispose()
        End Try
        Return cn
    End Function

    ''' <summary>Checks that the current AppSession is accepted by the database.</summary>
    Public Shared Sub ValidateSession()
        Using cn As SqlConnection = OpenSession()
        End Using
    End Sub

    ' ---------- parameters ----------

    Public Shared Function P(name As String, value As Object) As SqlParameter
        Return New SqlParameter(name, If(value, CObj(DBNull.Value)))
    End Function

    ''' <summary>VARBINARY parameter (typed, so NULL works).</summary>
    Public Shared Function PBin(name As String, value As Byte()) As SqlParameter
        Dim prm As New SqlParameter(name, SqlDbType.VarBinary, 256)
        prm.Value = If(value Is Nothing, CObj(DBNull.Value), CObj(value))
        Return prm
    End Function

    ''' <summary>Table-valued parameter (typeName = the SQL table type, e.g. att.MonthlyAttendanceType).</summary>
    Public Shared Function PTable(name As String, value As DataTable, typeName As String) As SqlParameter
        Dim prm As New SqlParameter(name, SqlDbType.Structured)
        prm.TypeName = typeName
        prm.Value = value
        Return prm
    End Function

    ' ---------- execution ----------

    Public Shared Function Table(cn As SqlConnection, proc As String, ParamArray args As SqlParameter()) As DataTable
        Dim dt As New DataTable()
        Run(Sub()
                Using cmd As SqlCommand = Build(cn, proc, args)
                    Using da As New SqlDataAdapter(cmd)
                        da.Fill(dt)
                    End Using
                End Using
            End Sub)
        Return dt
    End Function

    Public Shared Function Scalar(cn As SqlConnection, proc As String, ParamArray args As SqlParameter()) As Object
        Dim result As Object = Nothing
        Run(Sub()
                Using cmd As SqlCommand = Build(cn, proc, args)
                    result = cmd.ExecuteScalar()
                End Using
            End Sub)
        Return result
    End Function

    Public Shared Sub Exec(cn As SqlConnection, proc As String, ParamArray args As SqlParameter())
        Run(Sub()
                Using cmd As SqlCommand = Build(cn, proc, args)
                    cmd.ExecuteNonQuery()
                End Using
            End Sub)
    End Sub

    ' ---------- value readers ----------

    Public Shared Function IntOrNull(v As Object) As Integer?
        If v Is Nothing OrElse v Is DBNull.Value Then Return Nothing
        Return Convert.ToInt32(v)
    End Function

    Public Shared Function DateOrNull(v As Object) As Date?
        If v Is Nothing OrElse v Is DBNull.Value Then Return Nothing
        Return Convert.ToDateTime(v)
    End Function

    Public Shared Function StrOrNull(v As Object) As String
        If v Is Nothing OrElse v Is DBNull.Value Then Return Nothing
        Return Convert.ToString(v)
    End Function

    Public Shared Function BytesOrNull(v As Object) As Byte()
        If v Is Nothing OrElse v Is DBNull.Value Then Return Nothing
        Return DirectCast(v, Byte())
    End Function

    ' ---------- internals ----------

    Private Shared Function Build(cn As SqlConnection, proc As String, ParamArray args As SqlParameter()) As SqlCommand
        Dim cmd As New SqlCommand(proc, cn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.CommandTimeout = COMMAND_TIMEOUT
        If args IsNot Nothing AndAlso args.Length > 0 Then cmd.Parameters.AddRange(args)
        Return cmd
    End Function

    Private Shared Sub Run(action As Action)
        Try
            action()
        Catch ex As SqlException
            Throw Translate(ex)
        End Try
    End Sub

    ''' <summary>Proc errors THROWn with number >= 50000 are business messages; the rest are logged.</summary>
    Friend Shared Function Translate(ex As SqlException) As Exception
        If ex.Number >= 50000 Then Return New BusinessException(ex.Message, ex)
        Logger.Failure("SQL error " & ex.Number, ex)
        Select Case ex.Number
            Case -1, 2, 26, 40, 53, 233, 10060, 10061
                Return New BusinessException("Cannot reach the database server. Check the server PC and network.", ex)
            Case 18456, 4060
                Return New BusinessException("Database login failed. Check the connection settings.", ex)
            Case 2812
                Return New BusinessException("Database scripts are not applied. Run db scripts 0001 to 0013 first.", ex)
            Case 1205
                Return New BusinessException("The database was busy. Please try again.", ex)
            Case Else
                Return New BusinessException("Database error (" & ex.Number & "). Details were logged.", ex)
        End Select
    End Function
End Class