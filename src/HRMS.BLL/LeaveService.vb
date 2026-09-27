Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>Leave application workflow and balance. Apply/List/Cancel/GetBalance are left open to any
''' logged-in user here because att.usp_Leave_Apply etc. already restrict an Employee-type login to
''' their own EmployeeID (same self-check shape as usp_Employee_Get) - the stored proc throws
''' "Permission denied" if someone tries to act outside that, so this layer does not need to know
''' which EmployeeID belongs to the current session. Approve/Reject/SavePolicy have no self path, so
''' they are required here too (fail fast, same as the rest of this codebase).</summary>
Public NotInheritable Class LeaveService
    Private Shared ReadOnly VALID_TYPES As String() = {"CL", "SL", "EL", "ML", "LWP"}

    Public Function List(employeeID As Integer?, status As String, year As Integer?) As DataTable
        AppSession.Require().RequireCompany()
        Return LeaveRepo.List(employeeID, status, year)
    End Function

    Public Function Apply(employeeID As Integer, leaveType As String, fromDate As Date, toDate As Date, reason As String) As Integer
        Dim s As UserSession = AppSession.Require()
        s.RequireCompany()
        If Array.IndexOf(VALID_TYPES, leaveType) < 0 Then Throw New BusinessException("Invalid leave type.")
        If toDate < fromDate Then Throw New BusinessException("End date is before the start date.")
        Dim reasonTrimmed As String = If(String.IsNullOrWhiteSpace(reason), Nothing, reason.Trim())
        Dim id As Integer = LeaveRepo.Apply(employeeID, leaveType, fromDate, toDate, reasonTrimmed)
        Logger.Info("Leave applied: employee " & employeeID & " " & leaveType & " " &
                    fromDate.ToString("yyyy-MM-dd") & " to " & toDate.ToString("yyyy-MM-dd") & " by " & s.Username)
        Return id
    End Function

    Public Sub Approve(leaveID As Integer, note As String)
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_EDIT")
        LeaveRepo.SetStatus(leaveID, "Approved", If(String.IsNullOrWhiteSpace(note), Nothing, note.Trim()))
        Logger.Info("Leave " & leaveID & " approved by " & s.Username)
    End Sub

    Public Sub Reject(leaveID As Integer, note As String)
        Dim s As UserSession = AppSession.Require()
        s.Require("ATT_EDIT")
        If String.IsNullOrWhiteSpace(note) Then Throw New BusinessException("Give a reason for rejecting.")
        LeaveRepo.SetStatus(leaveID, "Rejected", note.Trim())
        Logger.Info("Leave " & leaveID & " rejected by " & s.Username & ": " & note.Trim())
    End Sub

    Public Sub Cancel(leaveID As Integer)
        Dim s As UserSession = AppSession.Require()
        s.RequireCompany()
        LeaveRepo.Cancel(leaveID)
        Logger.Info("Leave " & leaveID & " cancelled by " & s.Username)
    End Sub

    Public Function GetBalance(employeeID As Integer, asOfDate As Date?) As LeaveBalance
        AppSession.Require().RequireCompany()
        Return LeaveRepo.GetBalance(employeeID, asOfDate)
    End Function

    Public Function GetPolicy() As LeavePolicy
        AppSession.Require().RequireCompany()
        Return LeaveRepo.GetPolicy()
    End Function

    Public Sub SavePolicy(p As LeavePolicy)
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_EDIT")
        s.RequireCompany()
        If p Is Nothing OrElse p.CLAnnualDays < 0D OrElse p.SLAnnualDays < 0D Then
            Throw New BusinessException("Leave quota cannot be negative.")
        End If
        If p.ELDivisorDays <= 0 Then Throw New BusinessException("EL divisor must be greater than zero.")
        LeaveRepo.SavePolicy(p)
        Logger.Info("Leave policy updated by " & s.Username)
    End Sub
End Class
