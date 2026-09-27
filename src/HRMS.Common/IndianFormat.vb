Option Strict On
Option Explicit On

Imports System
Imports System.Globalization
Imports System.Text

''' <summary>Indian currency and digit grouping (lakh/crore), independent of Windows regional settings.</summary>
Public NotInheritable Class IndianFormat
    Public Shared ReadOnly RUPEE_SYMBOL As String = Char.ConvertFromUtf32(&H20B9)

    Private Sub New()
    End Sub

    ''' <summary>1234567.5 -> "₹ 12,34,567.50"</summary>
    Public Shared Function Rupees(amount As Decimal, Optional withSymbol As Boolean = True) As String
        Dim r As Decimal = Math.Round(amount, 2, MidpointRounding.AwayFromZero)
        Dim negative As Boolean = r < 0D
        r = Math.Abs(r)
        Dim s As String = r.ToString("0.00", CultureInfo.InvariantCulture)
        Dim dot As Integer = s.IndexOf("."c)
        Dim text As String = GroupIndian(s.Substring(0, dot)) & s.Substring(dot)
        If withSymbol Then text = RUPEE_SYMBOL & " " & text
        If negative Then text = "-" & text
        Return text
    End Function

    Private Shared Function GroupIndian(digits As String) As String
        If digits.Length <= 3 Then Return digits
        Dim head As String = digits.Substring(0, digits.Length - 3)
        Dim tail As String = digits.Substring(digits.Length - 3)
        Dim sb As New StringBuilder()
        Dim start As Integer = head.Length Mod 2
        If start = 1 Then sb.Append(head(0))
        For i As Integer = start To head.Length - 1 Step 2
            If sb.Length > 0 Then sb.Append(","c)
            sb.Append(head, i, 2)
        Next
        sb.Append(","c).Append(tail)
        Return sb.ToString()
    End Function
End Class
