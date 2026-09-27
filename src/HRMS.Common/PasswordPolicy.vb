Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class PasswordPolicy
    Public Const MIN_LENGTH As Integer = 8
    Private Shared ReadOnly COMMON_PASSWORDS As String() = {"password", "password1", "12345678", "123456789", "qwerty123", "admin123", "welcome1", "letmein1"}

    Private Sub New()
    End Sub

    ''' <summary>Returns an error message, or Nothing when the password is acceptable.</summary>
    Public Shared Function Validate(password As String, username As String) As String
        If String.IsNullOrEmpty(password) OrElse password.Length < MIN_LENGTH Then
            Return "Password must be at least " & MIN_LENGTH & " characters."
        End If
        Dim hasLetter As Boolean = False
        Dim hasDigit As Boolean = False
        For Each ch As Char In password
            If Char.IsLetter(ch) Then hasLetter = True
            If Char.IsDigit(ch) Then hasDigit = True
        Next
        If Not (hasLetter AndAlso hasDigit) Then Return "Password must contain letters and digits."
        If Not String.IsNullOrEmpty(username) AndAlso String.Equals(password, username, StringComparison.OrdinalIgnoreCase) Then
            Return "Password must not equal the username."
        End If
        If Array.IndexOf(COMMON_PASSWORDS, password.ToLowerInvariant()) >= 0 Then Return "Password is too common."
        Return Nothing
    End Function
End Class
