Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class MonthStatus
    Public Property IsClosed As Boolean
    Public Property ClosedOn As Date?
    Public Property ClosedByName As String
    Public Property ReopenReason As String
End Class
