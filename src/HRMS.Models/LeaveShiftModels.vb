Option Strict On
Option Explicit On

Imports System

''' <summary>Shift master (att.Shift). StartTime/EndTime map to SQL TIME as TimeSpan.</summary>
Public Class ShiftInfo
    Public Property ShiftID As Integer?
    Public Property Name As String
    Public Property StartTime As TimeSpan
    Public Property EndTime As TimeSpan
    Public Property ShiftHours As Decimal
    Public Property IsActive As Boolean = True
End Class

''' <summary>One employee's attendance for one day (att.Attendance). 'L' is intentionally not offered
''' by the daily-entry screen - leave is applied through LeaveApplication instead.</summary>
Public Class DailyAttendanceEntry
    Public Property EmployeeID As Integer
    Public Property EmpCode As String
    Public Property Status As String
    Public Property ShiftID As Integer?
    Public Property InTime As TimeSpan?
    Public Property OutTime As TimeSpan?
    Public Property OTHours As Decimal
End Class

''' <summary>A leave application (att.Leave). LeaveType: CL/SL/EL/ML/LWP. Status: Pending/Approved/Rejected.</summary>
Public Class LeaveApplication
    Public Property LeaveID As Integer?
    Public Property EmployeeID As Integer
    Public Property EmpCode As String
    Public Property EmployeeName As String
    Public Property LeaveType As String
    Public Property FromDate As Date
    Public Property ToDate As Date
    Public Property Days As Decimal
    Public Property Status As String
    Public Property Reason As String
    Public Property CreatedOn As Date?
End Class

''' <summary>CL/SL/EL balance as of a date (att.usp_LeaveBalance_Get). CL/SL are a fixed annual quota;
''' EL follows the Factories Act 1:divisor rule - see LeavePolicy.</summary>
Public Class LeaveBalance
    Public Property CLEntitlement As Decimal
    Public Property CLTaken As Decimal
    Public Property CLBalance As Decimal
    Public Property SLEntitlement As Decimal
    Public Property SLTaken As Decimal
    Public Property SLBalance As Decimal
    Public Property ELEarned As Decimal
    Public Property ELTaken As Decimal
    Public Property ELBalance As Decimal
    Public Property AsOfDate As Date
End Class

''' <summary>Company-configurable leave policy (cfg.Setting: LEAVE_CL_ANNUAL, LEAVE_SL_ANNUAL, LEAVE_EL_DIVISOR).</summary>
Public Class LeavePolicy
    Public Property CLAnnualDays As Decimal
    Public Property SLAnnualDays As Decimal
    Public Property ELDivisorDays As Integer
End Class
