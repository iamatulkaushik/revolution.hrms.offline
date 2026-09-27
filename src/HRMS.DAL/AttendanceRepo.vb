Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports HRMS.Models

Public NotInheritable Class AttendanceRepo
    Private Sub New()
    End Sub

    Public Shared Function ListMonth(year As Integer, month As Integer, factoryID As Integer?) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "att.usp_Monthly_List", Db.P("@Year", year), Db.P("@Month", month), Db.P("@FactoryID", factoryID))
        End Using
    End Function

    ''' <summary>Saves every entry in one transaction (table-valued parameter). Returns the number of rows saved.</summary>
    Public Shared Function SaveBatch(year As Integer, month As Integer, entries As IList(Of AttendanceEntry)) As Integer
        Dim t As New DataTable()
        t.Columns.Add("EmployeeID", GetType(Integer))
        t.Columns.Add("FactoryID", GetType(Integer))
        t.Columns.Add("WorkingDays", GetType(Decimal))
        t.Columns.Add("Holidays", GetType(Decimal))
        t.Columns.Add("CasualLeave", GetType(Decimal))
        t.Columns.Add("EarnedLeave", GetType(Decimal))
        t.Columns.Add("SickLeave", GetType(Decimal))
        t.Columns.Add("CompLeave", GetType(Decimal))
        t.Columns.Add("OTHours", GetType(Decimal))
        t.Columns.Add("Remarks", GetType(String))
        For Each e As AttendanceEntry In entries
            t.Rows.Add(e.EmployeeID, If(e.FactoryID.HasValue, CObj(e.FactoryID.Value), DBNull.Value),
                       e.WorkingDays, e.Holidays, e.CasualLeave, e.EarnedLeave, e.SickLeave, e.CompLeave, e.OTHours,
                       If(String.IsNullOrEmpty(e.Remarks), DBNull.Value, CObj(e.Remarks)))
        Next
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "att.usp_Monthly_SaveBatch", Db.P("@Year", year), Db.P("@Month", month),
                                             Db.PTable("@Rows", t, "att.MonthlyAttendanceType")))
        End Using
    End Function

    Public Shared Function GetStatus(year As Integer, month As Integer) As MonthStatus
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "att.usp_MonthClose_Get", Db.P("@Year", year), Db.P("@Month", month))
            Dim s As New MonthStatus()
            If dt.Rows.Count = 0 Then Return s
            Dim r As DataRow = dt.Rows(0)
            s.IsClosed = Convert.ToBoolean(r("IsClosed"))
            s.ClosedOn = Db.DateOrNull(r("ClosedOn"))
            s.ClosedByName = Db.StrOrNull(r("ClosedByName"))
            s.ReopenReason = Db.StrOrNull(r("ReopenReason"))
            Return s
        End Using
    End Function

    Public Shared Sub CloseMonth(year As Integer, month As Integer)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "att.usp_MonthClose_Close", Db.P("@Year", year), Db.P("@Month", month))
        End Using
    End Sub

    Public Shared Sub ReopenMonth(year As Integer, month As Integer, reason As String)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "att.usp_MonthClose_Reopen", Db.P("@Year", year), Db.P("@Month", month), Db.P("@Reason", reason))
        End Using
    End Sub

    ' ---------- Attendance mode (cfg.Setting: Daily or Monthly) ----------
    Public Shared Function GetMode() As String
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "cfg.usp_AttendanceMode_Get")
            Return If(dt.Rows.Count > 0, Convert.ToString(dt.Rows(0)("Mode")), "Monthly")
        End Using
    End Function

    Public Shared Sub SetMode(mode As String)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "cfg.usp_AttendanceMode_Set", Db.P("@Mode", mode))
        End Using
    End Sub

    ' ---------- Daily attendance (att.Attendance, one date at a time) ----------
    Public Shared Function ListDay(attDate As Date, factoryID As Integer?) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "att.usp_DailyAttendance_List", Db.P("@AttDate", attDate), Db.P("@FactoryID", factoryID))
        End Using
    End Function

    ''' <summary>Saves every row for one date in one transaction (table-valued parameter).</summary>
    Public Shared Function SaveDayBatch(attDate As Date, entries As IList(Of DailyAttendanceEntry)) As Integer
        Dim t As New DataTable()
        t.Columns.Add("EmployeeID", GetType(Integer))
        t.Columns.Add("Status", GetType(String))
        t.Columns.Add("ShiftID", GetType(Integer))
        t.Columns.Add("InTime", GetType(TimeSpan))
        t.Columns.Add("OutTime", GetType(TimeSpan))
        t.Columns.Add("OTHours", GetType(Decimal))
        For Each e As DailyAttendanceEntry In entries
            t.Rows.Add(e.EmployeeID, e.Status,
                       If(e.ShiftID.HasValue, CObj(e.ShiftID.Value), DBNull.Value),
                       If(e.InTime.HasValue, CObj(e.InTime.Value), DBNull.Value),
                       If(e.OutTime.HasValue, CObj(e.OutTime.Value), DBNull.Value),
                       e.OTHours)
        Next
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "att.usp_DailyAttendance_SaveBatch", Db.P("@AttDate", attDate),
                                             Db.PTable("@Rows", t, "att.DailyAttendanceType")))
        End Using
    End Function
End Class
