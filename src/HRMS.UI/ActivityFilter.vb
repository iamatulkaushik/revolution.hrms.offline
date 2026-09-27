Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms

''' <summary>Watches keyboard/mouse messages so the main form can lock after inactivity.</summary>
Friend NotInheritable Class ActivityFilter
    Implements IMessageFilter

    Private Const WM_KEYDOWN As Integer = &H100
    Private Const WM_KEYUP As Integer = &H101
    Private Const WM_MOUSEMOVE As Integer = &H200
    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_RBUTTONDOWN As Integer = &H204
    Private Const WM_MOUSEWHEEL As Integer = &H20A

    Private _lastUtc As Date = Date.UtcNow

    Public ReadOnly Property IdleFor As TimeSpan
        Get
            Return Date.UtcNow - _lastUtc
        End Get
    End Property

    Public Sub Touch()
        _lastUtc = Date.UtcNow
    End Sub

    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        Select Case m.Msg
            Case WM_KEYDOWN, WM_KEYUP, WM_MOUSEMOVE, WM_LBUTTONDOWN, WM_RBUTTONDOWN, WM_MOUSEWHEEL
                Touch()
        End Select
        Return False
    End Function
End Class
