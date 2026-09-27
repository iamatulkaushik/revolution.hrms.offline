Option Strict On
Option Explicit On

Imports System

''' <summary>One employee's attendance for one month. Days are whole or half days.</summary>
Public NotInheritable Class AttendanceEntry
    Public Property EmployeeID As Integer
    ''' <summary>Shown in messages only.</summary>
    Public Property EmpCode As String
    Public Property FactoryID As Integer?
    Public Property WorkingDays As Decimal
    ''' <summary>Paid weekly offs and holidays.</summary>
    Public Property Holidays As Decimal
    Public Property CasualLeave As Decimal
    Public Property EarnedLeave As Decimal
    Public Property SickLeave As Decimal
    Public Property CompLeave As Decimal
    Public Property OTHours As Decimal
    Public Property Remarks As String
End Class
