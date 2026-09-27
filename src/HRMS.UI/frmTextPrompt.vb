Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Asks for a line of text with a minimum length (e.g. the reason for reopening a month).</summary>
Friend Class frmTextPrompt
    Inherits BaseForm

    Private ReadOnly txt As TextBox = UiKit.MakeText()
    Private ReadOnly btnOk As Button = UiKit.MakeButton("OK", True)
    Private ReadOnly btnCancel As Button = UiKit.MakeButton("Cancel", False)
    Private ReadOnly lblMessage As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly _minLength As Integer

    Public ReadOnly Property Value As String
        Get
            Return txt.Text.Trim()
        End Get
    End Property

    Public Sub New(title As String, prompt As String, minLength As Integer, maxLength As Integer)
        _minLength = minLength
        Text = ErrorHandler.APP_TITLE & " - " & title
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(460, 240)
        Controls.Add(UiKit.MakeHeader(title, Nothing, 460, 56))
        txt.MaxLength = maxLength
        txt.Multiline = True
        txt.Height = 70
        UiKit.PlaceField(Me, prompt, 24, 68, txt, 24, 92, 412)
        lblMessage.ForeColor = Theme.Danger
        lblMessage.AutoSize = False
        lblMessage.SetBounds(24, 166, 412, 20)
        Controls.Add(lblMessage)
        btnOk.Location = New Point(206, 192)
        btnCancel.Location = New Point(326, 192)
        Controls.Add(btnOk)
        Controls.Add(btnCancel)
        CancelButton = btnCancel
        btnCancel.DialogResult = DialogResult.Cancel
        UiKit.AttachFocusColor(Me)
        EnterMovesFocus = False
        AddHandler btnOk.Click, AddressOf OnOkClick
    End Sub

    Private Sub OnOkClick(sender As Object, e As EventArgs)
        If txt.Text.Trim().Length < _minLength Then
            lblMessage.Text = "Please enter at least " & _minLength & " characters."
            Return
        End If
        DialogResult = DialogResult.OK
    End Sub
End Class
