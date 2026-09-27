Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>Monthly attendance: load, save, close month, reopen month, import.</summary>
Public NotInheritable Class AttendanceService
    Private Const MAX_MESSAGES As Integer = 12

    ''' <summary>Everyone who could have worked in the month, with their saved days (zeros if none yet).</summary>
    Public Function Load(year As Integer, month As Integer, factoryID As Integer?) As DataTable
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_VIEW")
        s.RequireCompany()
        CheckPeriod(year, month)
        Return AttendanceRepo.ListMonth(year, month, factoryID)
    End Function

    Public Function GetStatus(year As Integer, month As Integer) As MonthStatus
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_VIEW")
        s.RequireCompany()
        Return AttendanceRepo.GetStatus(year, month)
    End Function

    ''' <summary>Validates every entry, then saves all of them in one transaction. Returns the number saved.</summary>
    Public Function Save(year As Integer, month As Integer, entries As IList(Of AttendanceEntry)) As Integer
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_EDIT")
        s.RequireCompany()
        CheckPeriod(year, month)
        Dim problems As New List(Of String)()
        For Each e As AttendanceEntry In entries
            problems.AddRange(AttendanceEngine.Validate(e, year, month))
        Next
        If problems.Count > 0 Then Throw New BusinessException(Summarise(problems))
        If entries.Count = 0 Then Return 0
        Dim saved As Integer = AttendanceRepo.SaveBatch(year, month, entries)
        Logger.Info("Attendance saved " & month & "/" & year & ": " & saved & " rows by " & s.Username)
        Return saved
    End Function

    Public Sub CloseMonth(year As Integer, month As Integer)
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_CLOSE")
        s.RequireCompany()
        CheckPeriod(year, month)
        AttendanceRepo.CloseMonth(year, month)
        Logger.Info("Attendance month closed " & month & "/" & year & " by " & s.Username)
    End Sub

    Public Sub ReopenMonth(year As Integer, month As Integer, reason As String)
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_CLOSE")
        s.RequireCompany()
        CheckPeriod(year, month)
        If String.IsNullOrWhiteSpace(reason) OrElse reason.Trim().Length < 10 Then
            Throw New BusinessException("Give a reason of at least 10 characters.")
        End If
        AttendanceRepo.ReopenMonth(year, month, reason.Trim())
        Logger.Warn("Attendance month REOPENED " & month & "/" & year & " by " & s.Username & ": " & reason.Trim())
    End Sub

    Public Function ParseImport(rows As List(Of String()), codeToId As IDictionary(Of String, Integer),
                                year As Integer, month As Integer) As ImportResult
        AppSession.Require().Require("ATT_EDIT")
        Return AttendanceImporter.Parse(rows, codeToId, year, month)
    End Function

    ''' <summary>Whether this company runs Daily or Monthly attendance. Phase 6 payroll should read
    ''' totals through att.usp_Monthly_GetEffective (mode-aware) rather than picking a table directly.</summary>
    Public Function GetMode() As String
        AppSession.Require().RequireCompany()
        Return AttendanceRepo.GetMode()
    End Function

    Public Sub SetMode(mode As String)
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_EDIT")
        s.RequireCompany()
        If mode <> "Daily" AndAlso mode <> "Monthly" Then Throw New BusinessException("Mode must be Daily or Monthly.")
        AttendanceRepo.SetMode(mode)
        Logger.Info("Attendance mode set to " & mode & " by " & s.Username)
    End Sub

    ''' <summary>Everyone who could have worked that day, with their saved entry (blank if none yet).</summary>
    Public Function LoadDay(attDate As Date, factoryID As Integer?) As DataTable
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_VIEW")
        s.RequireCompany()
        Return AttendanceRepo.ListDay(attDate, factoryID)
    End Function

    ''' <summary>Validates every row, then saves all of them in one transaction. Returns the number saved.</summary>
    Public Function SaveDay(attDate As Date, entries As IList(Of DailyAttendanceEntry)) As Integer
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_EDIT")
        s.RequireCompany()
        Dim problems As New List(Of String)()
        For Each e As DailyAttendanceEntry In entries
            problems.AddRange(AttendanceEngine.ValidateDaily(e))
        Next
        If problems.Count > 0 Then Throw New BusinessException(Summarise(problems))
        If entries.Count = 0 Then Return 0
        Dim saved As Integer = AttendanceRepo.SaveDayBatch(attDate, entries)
        Logger.Info("Daily attendance saved " & attDate.ToString("yyyy-MM-dd") & ": " & saved & " rows by " & s.Username)
        Return saved
    End Function

    Private Shared Sub CheckPeriod(year As Integer, month As Integer)
        If month < 1 OrElse month > 12 OrElse year < 2000 OrElse year > 2100 Then
            Throw New BusinessException("Invalid month or year.")
        End If
    End Sub

    Private Shared Function Summarise(messages As List(Of String)) As String
        Dim shown As IEnumerable(Of String) = messages
        If messages.Count > MAX_MESSAGES Then shown = messages.GetRange(0, MAX_MESSAGES)
        Dim text As String = String.Join(Environment.NewLine, shown)
        If messages.Count > MAX_MESSAGES Then text &= Environment.NewLine & "... and " & (messages.Count - MAX_MESSAGES) & " more."
        Return text
    End Function
End Class
