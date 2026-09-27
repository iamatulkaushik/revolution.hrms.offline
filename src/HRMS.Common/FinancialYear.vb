Option Strict On
Option Explicit On

Imports System
Imports System.Globalization

''' <summary>Indian financial year: 1 April to 31 March.</summary>
Public NotInheritable Class FinancialYear
    Private Sub New()
    End Sub

    Public Shared Function StartYear(d As Date) As Integer
        Return If(d.Month >= 4, d.Year, d.Year - 1)
    End Function

    Public Shared Function FromDate(d As Date) As Date
        Return New Date(StartYear(d), 4, 1)
    End Function

    Public Shared Function ToDate(d As Date) As Date
        Return New Date(StartYear(d) + 1, 3, 31)
    End Function

    ''' <summary>"2026-27"</summary>
    Public Shared Function FyName(d As Date) As String
        Dim y As Integer = StartYear(d)
        Return y.ToString(CultureInfo.InvariantCulture) & "-" & ((y + 1) Mod 100).ToString("00", CultureInfo.InvariantCulture)
    End Function
End Class
