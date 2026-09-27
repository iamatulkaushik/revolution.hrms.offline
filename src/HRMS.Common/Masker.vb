Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class Masker
    Private Sub New()
    End Sub

    ''' <summary>Keeps the last N characters, masks the rest: XXXXXXXX1234.</summary>
    Public Shared Function Mask(value As String, Optional visible As Integer = 4) As String
        If String.IsNullOrEmpty(value) Then Return String.Empty
        If value.Length <= visible Then Return New String("X"c, value.Length)
        Return New String("X"c, value.Length - visible) & value.Substring(value.Length - visible)
    End Function
End Class
