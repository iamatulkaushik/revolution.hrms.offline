Option Strict On
Option Explicit On

Imports System
Imports System.Text.RegularExpressions

''' <summary>Format checks for Indian identifiers. Return True when the value is valid.</summary>
Public NotInheritable Class Validators
    Private Shared ReadOnly PAN_RX As New Regex("^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.Compiled)
    Private Shared ReadOnly IFSC_RX As New Regex("^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.Compiled)
    Private Shared ReadOnly MOBILE_RX As New Regex("^[6-9][0-9]{9}$", RegexOptions.Compiled)
    Private Shared ReadOnly UAN_RX As New Regex("^[0-9]{12}$", RegexOptions.Compiled)
    Private Shared ReadOnly ESI_IP_RX As New Regex("^([0-9]{10}|[0-9]{17})$", RegexOptions.Compiled)
    Private Shared ReadOnly BANK_RX As New Regex("^[0-9]{9,18}$", RegexOptions.Compiled)
    Private Shared ReadOnly EMAIL_RX As New Regex("^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)
    Private Shared ReadOnly AADHAAR_RX As New Regex("^[2-9][0-9]{11}$", RegexOptions.Compiled)

    ' Verhoeff checksum tables (used by Aadhaar)
    Private Shared ReadOnly VD As Integer(,) = New Integer(,) {
        {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, {1, 2, 3, 4, 0, 6, 7, 8, 9, 5},
        {2, 3, 4, 0, 1, 7, 8, 9, 5, 6}, {3, 4, 0, 1, 2, 8, 9, 5, 6, 7},
        {4, 0, 1, 2, 3, 9, 5, 6, 7, 8}, {5, 9, 8, 7, 6, 0, 4, 3, 2, 1},
        {6, 5, 9, 8, 7, 1, 0, 4, 3, 2}, {7, 6, 5, 9, 8, 2, 1, 0, 4, 3},
        {8, 7, 6, 5, 9, 3, 2, 1, 0, 4}, {9, 8, 7, 6, 5, 4, 3, 2, 1, 0}}
    Private Shared ReadOnly VP As Integer(,) = New Integer(,) {
        {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, {1, 5, 7, 6, 2, 8, 3, 0, 9, 4},
        {5, 8, 0, 3, 7, 9, 6, 1, 4, 2}, {8, 9, 1, 6, 0, 4, 3, 5, 2, 7},
        {9, 4, 5, 3, 1, 2, 6, 8, 7, 0}, {4, 2, 8, 6, 5, 7, 3, 9, 0, 1},
        {2, 7, 9, 3, 8, 0, 6, 4, 1, 5}, {7, 0, 4, 6, 9, 1, 3, 2, 5, 8}}

    Private Sub New()
    End Sub

    Public Shared Function IsValidEmail(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso value.Length <= 80 AndAlso EMAIL_RX.IsMatch(value)
    End Function

    Public Shared Function IsValidPan(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso PAN_RX.IsMatch(value)
    End Function

    Public Shared Function IsValidIfsc(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso IFSC_RX.IsMatch(value)
    End Function

    Public Shared Function IsValidMobile(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso MOBILE_RX.IsMatch(value)
    End Function

    Public Shared Function IsValidUan(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso UAN_RX.IsMatch(value)
    End Function

    Public Shared Function IsValidEsiIp(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso ESI_IP_RX.IsMatch(value)
    End Function

    Public Shared Function IsValidBankAccount(value As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso BANK_RX.IsMatch(value)
    End Function

    ''' <summary>12 digits, first digit 2-9, Verhoeff checksum.</summary>
    Public Shared Function IsValidAadhaar(value As String) As Boolean
        If String.IsNullOrEmpty(value) OrElse Not AADHAAR_RX.IsMatch(value) Then Return False
        Dim c As Integer = 0
        Dim n As Integer = 0
        For i As Integer = value.Length - 1 To 0 Step -1
            Dim digit As Integer = Convert.ToInt32(value(i)) - Convert.ToInt32("0"c)
            c = VD(c, VP(n Mod 8, digit))
            n += 1
        Next
        Return c = 0
    End Function
End Class
