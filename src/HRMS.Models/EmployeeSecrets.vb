Option Strict On
Option Explicit On

Imports System

''' <summary>Plain-text sensitive values. Exists only in memory; empty/Nothing = leave unchanged on save.</summary>
Public NotInheritable Class EmployeeSecrets
    Public Property Aadhaar As String
    Public Property PAN As String
    Public Property BankAcc As String
    Public Property UAN As String
    Public Property EsiIP As String
End Class
