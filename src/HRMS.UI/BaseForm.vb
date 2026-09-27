Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Common look and keyboard behaviour: Enter moves to the next field (design.md hotkeys).</summary>
Friend Class BaseForm
    Inherits Form

    Protected Property EnterMovesFocus As Boolean = True

    Public Sub New()
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        Font = Theme.BaseFont
        BackColor = Theme.Background
        KeyPreview = True
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.Handled OrElse Not EnterMovesFocus Then Return
        If e.KeyCode = Keys.Enter AndAlso Not e.Control AndAlso Not e.Alt Then
            If MoveToNextField() Then
                e.Handled = True
                e.SuppressKeyPress = True
            End If
        End If
    End Sub

    ''' <summary>Enter on a field: go to the next field, or press the default button after the last field.</summary>
    Private Function MoveToNextField() As Boolean
        Dim active As Control = ActiveControl
        Do While TypeOf active Is ContainerControl AndAlso DirectCast(active, ContainerControl).ActiveControl IsNot Nothing
            active = DirectCast(active, ContainerControl).ActiveControl
        Loop
        If active Is Nothing OrElse TypeOf active Is Button Then Return False
        If TypeOf active Is DataGridView OrElse TypeOf active Is ListBox OrElse TypeOf active Is ListView Then Return False
        Dim tb As TextBoxBase = TryCast(active, TextBoxBase)
        If tb IsNot Nothing AndAlso tb.Multiline Then Return False

        Dim nxt As Control = NextInput(active)
        If nxt Is Nothing Then
            If AcceptButton Is Nothing Then Return False
            AcceptButton.PerformClick()
            Return True
        End If
        If TypeOf nxt Is Button AndAlso Object.ReferenceEquals(nxt, AcceptButton) Then
            AcceptButton.PerformClick()
            Return True
        End If
        nxt.Focus()
        Return True
    End Function

    Private Function NextInput(current As Control) As Control
        Dim c As Control = GetNextControl(current, True)
        Dim guard As Integer = 0
        Do While c IsNot Nothing AndAlso guard < 100
            If c.Visible AndAlso c.Enabled AndAlso c.TabStop AndAlso Not TypeOf c Is Label AndAlso c.CanFocus Then Return c
            c = GetNextControl(c, True)
            guard += 1
        Loop
        Return Nothing
    End Function

    Protected Sub ShowError(ex As Exception)
        ErrorHandler.Show(Me, ex)
    End Sub

    Protected Sub ShowInfo(text As String)
        ErrorHandler.ShowInfo(Me, text)
    End Sub

    Protected Function Confirm(text As String) As Boolean
        Return ErrorHandler.Confirm(Me, text)
    End Function
End Class
