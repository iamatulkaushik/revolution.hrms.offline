Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

''' <summary>Outcome of reading an attendance file: good rows and a message for each bad row.</summary>
Public NotInheritable Class ImportResult
    Public ReadOnly Property Entries As New List(Of AttendanceEntry)()
    Public ReadOnly Property Errors As New List(Of String)()
    Public Property RowsRead As Integer
End Class
