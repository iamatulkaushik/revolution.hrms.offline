Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports HRMS.Models

''' <summary>Pure attendance rules (no database). Paid days = worked + holidays + CL + EL + SL + comp leave.
''' Absent (unpaid) days = days in the month - paid days.</summary>
Public NotInheritable Class AttendanceEngine
    Public Const MAX_OT_HOURS_PER_DAY As Decimal = 24D

    Private Sub New()
    End Sub

    Public Shared Function DaysInMonth(year As Integer, month As Integer) As Integer
        Return Date.DaysInMonth(year, month)
    End Function

    Public Shared Function PaidDays(e As AttendanceEntry) As Decimal
        Return e.WorkingDays + e.Holidays + e.CasualLeave + e.EarnedLeave + e.SickLeave + e.CompLeave
    End Function

    Public Shared Function AbsentDays(e As AttendanceEntry, daysInMonth As Integer) As Decimal
        Return Math.Max(0D, daysInMonth - PaidDays(e))
    End Function

    ''' <summary>True for 0, 0.5, 1, 1.5 ...</summary>
    Public Shared Function IsHalfStep(value As Decimal) As Boolean
        Dim doubled As Decimal = value * 2D
        Return doubled = Math.Floor(doubled)
    End Function

    ''' <summary>Messages for one entry; empty list = valid.</summary>
    Public Shared Function Validate(e As AttendanceEntry, year As Integer, month As Integer) As List(Of String)
        Dim errors As New List(Of String)()
        Dim prefix As String = If(String.IsNullOrEmpty(e.EmpCode), String.Empty, e.EmpCode & ": ")
        CheckDays(errors, prefix, "Working days", e.WorkingDays)
        CheckDays(errors, prefix, "Holidays", e.Holidays)
        CheckDays(errors, prefix, "Casual leave", e.CasualLeave)
        CheckDays(errors, prefix, "Earned leave", e.EarnedLeave)
        CheckDays(errors, prefix, "Sick leave", e.SickLeave)
        CheckDays(errors, prefix, "Comp leave", e.CompLeave)
        Dim days As Integer = DaysInMonth(year, month)
        If e.OTHours < 0D Then errors.Add(prefix & "Overtime hours cannot be negative.")
        If e.OTHours > MAX_OT_HOURS_PER_DAY * days Then errors.Add(prefix & "Overtime hours are more than the hours in the month.")
        If PaidDays(e) > days Then errors.Add(prefix & PaidDays(e).ToString("0.#") & " days entered but the month has only " & days & ".")
        Return errors
    End Function

    Private Shared Sub CheckDays(errors As List(Of String), prefix As String, label As String, value As Decimal)
        If value < 0D Then
            errors.Add(prefix & label & " cannot be negative.")
        ElseIf Not IsHalfStep(value) Then
            errors.Add(prefix & label & " must be a whole or half day.")
        End If
    End Sub

    ''' <summary>Valid daily Status values. 'L' (from the 0003 schema) is deliberately excluded here -
    ''' leave is applied and tracked through LeaveApplication/LeaveService instead of a daily status.</summary>
    Public Shared ReadOnly DailyStatuses As String() = {"P", "A", "HD", "WO", "H"}

    ''' <summary>Messages for one daily-attendance row; empty list = valid.</summary>
    Public Shared Function ValidateDaily(e As DailyAttendanceEntry) As List(Of String)
        Dim errors As New List(Of String)()
        Dim prefix As String = If(String.IsNullOrEmpty(e.EmpCode), String.Empty, e.EmpCode & ": ")
        If Array.IndexOf(DailyStatuses, e.Status) < 0 Then
            errors.Add(prefix & "Status must be one of " & String.Join(", ", DailyStatuses) & ".")
        End If
        If e.OTHours < 0D Then errors.Add(prefix & "Overtime hours cannot be negative.")
        If e.OTHours > MAX_OT_HOURS_PER_DAY Then errors.Add(prefix & "Overtime hours cannot exceed 24 in a day.")
        Return errors
    End Function

    ''' <summary>Days of the month the employee could have worked (joining and leaving dates applied).</summary>
    Public Shared Function PayableWindow(year As Integer, month As Integer, joined As Date, left As Date?) As Integer
        Dim first As New Date(year, month, 1)
        Dim last As New Date(year, month, Date.DaysInMonth(year, month))
        Dim fromDate As Date = If(joined > first, joined, first)
        Dim toDate As Date = last
        If left.HasValue AndAlso left.Value < last Then toDate = left.Value
        If toDate < fromDate Then Return 0
        Return CInt((toDate.Date - fromDate.Date).TotalDays) + 1
    End Function
End Class
