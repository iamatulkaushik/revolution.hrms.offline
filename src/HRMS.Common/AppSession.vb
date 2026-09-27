Option Strict On
Option Explicit On

Imports System

''' <summary>Holds the current login for this process (one user per running app).</summary>
Public NotInheritable Class AppSession
    Private Shared _current As UserSession

    Private Sub New()
    End Sub

    Public Shared Property Current As UserSession
        Get
            Return _current
        End Get
        Set(value As UserSession)
            _current = value
        End Set
    End Property

    Public Shared ReadOnly Property IsLoggedIn As Boolean
        Get
            Return _current IsNot Nothing
        End Get
    End Property

    ''' <summary>Returns the session or throws if nobody is logged in.</summary>
    Public Shared Function Require() As UserSession
        If _current Is Nothing Then Throw New BusinessException("Please log in.")
        Return _current
    End Function

    Public Shared Sub Clear()
        _current = Nothing
    End Sub
End Class
