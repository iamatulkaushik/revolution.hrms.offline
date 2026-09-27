Option Strict On
Option Explicit On

Imports System

Public Enum LoginStatus
    Success = 0
    BadCredentials = 1
    Locked = 2
    Inactive = 3
End Enum

Public NotInheritable Class LoginOutcome
    Public Property Status As LoginStatus
    Public Property Message As String
    Public Property UserID As Integer
    Public Property UserType As String
    Public Property MustChangePassword As Boolean

    Public ReadOnly Property IsSuccess As Boolean
        Get
            Return Status = LoginStatus.Success
        End Get
    End Property
End Class
