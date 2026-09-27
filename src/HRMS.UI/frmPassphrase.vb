Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Asks for the passphrase that protects an exported/imported key file.</summary>
Friend Class frmPassphrase
    Inherits BaseForm

    Private Const MIN_LENGTH As Integer = 12
    Private ReadOnly txtPass As TextBox = UiKit.MakeText(True)
    Private ReadOnly txtConfirm As TextBox = UiKit.MakeText(True)
    Private ReadOnly btnOk As Button = UiKit.MakeButton("OK", True)
    Private ReadOnly btnCancel As Button = UiKit.MakeButton("Cancel", False)
    Private ReadOnly lblMessage As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly _confirm As Boolean

    Public ReadOnly Property Passphrase As String
        Get
            Return txtPass.Text
        End Get
    End Property

    Public Sub New(title As String, needConfirm As Boolean)
        _confirm = needConfirm
        Text = ErrorHandler.APP_TITLE & " - " & title
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(420, If(needConfirm, 290, 230))

        Controls.Add(UiKit.MakeHeader(title, "Minimum " & MIN_LENGTH & " characters. Keep it safe.", 420, 70))
        UiKit.PlaceField(Me, "Passphrase", 28, 90, txtPass, 28, 112, 364)
        Dim y As Integer = 148
        If needConfirm Then
            UiKit.PlaceField(Me, "Confirm passphrase", 28, 148, txtConfirm, 28, 170, 364)
            y = 206
        End If
        lblMessage.ForeColor = Theme.Danger
        lblMessage.AutoSize = False
        lblMessage.SetBounds(28, y - 8, 364, 20)
        Controls.Add(lblMessage)

        btnOk.Location = New Point(162, y + 12)
        btnCancel.Location = New Point(282, y + 12)
        Controls.Add(btnOk)
        Controls.Add(btnCancel)
        AcceptButton = btnOk
        CancelButton = btnCancel
        btnCancel.DialogResult = DialogResult.Cancel
        UiKit.AttachFocusColor(Me)
        AddHandler btnOk.Click, AddressOf OnOkClick
    End Sub

    Private Sub OnOkClick(sender As Object, e As EventArgs)
        If txtPass.Text.Length < MIN_LENGTH Then
            lblMessage.Text = "Passphrase must be at least " & MIN_LENGTH & " characters."
            Return
        End If
        If _confirm AndAlso txtPass.Text <> txtConfirm.Text Then
            lblMessage.Text = "Passphrases do not match."
            Return
        End If
        DialogResult = DialogResult.OK
    End Sub
End Class
