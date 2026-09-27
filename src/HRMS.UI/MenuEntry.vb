Option Strict On
Option Explicit On

Imports System

Friend NotInheritable Class MenuEntry
    Public ReadOnly Property Key As String
    Public ReadOnly Property Caption As String
    ''' <summary>Permission code, or codes separated by | (any one is enough), or Nothing for everyone.</summary>
    Public ReadOnly Property Permission As String
    Public ReadOnly Property Phase As String

    Public Sub New(key As String, caption As String, permission As String, phase As String)
        Me.Key = key
        Me.Caption = caption
        Me.Permission = permission
        Me.Phase = phase
    End Sub
End Class
