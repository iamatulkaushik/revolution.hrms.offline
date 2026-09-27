Option Strict On
Option Explicit On

Imports System

''' <summary>Expected, user-facing error (validation, permission, duplicate...).</summary>
Public Class BusinessException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
