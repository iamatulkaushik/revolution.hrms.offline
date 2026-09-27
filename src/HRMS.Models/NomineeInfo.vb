Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class NomineeInfo
    Public Property NomineeID As Integer?
    Public Property EmployeeID As Integer
    Public Property Name As String
    Public Property Relation As String
    Public Property DOB As Date?
    Public Property SharePct As Decimal
End Class
