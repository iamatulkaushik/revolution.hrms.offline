Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>forced = first login or reset: the user cannot skip it (Cancel becomes "Log out").</summary>
Friend Class frmChangePassword
    Inherits BaseForm

    Private ReadOnly txtCurrent As TextBox = UiKit.MakeText(True)
    Private ReadOnly txtNew As TextBox = UiKit.MakeText(True)
    Private ReadOnly txtConfirm As TextBox = UiKit.MakeText(True)
    Private ReadOnly btnOk As Button = UiKit.MakeButton("Change", True)
    Private ReadOnly btnCancel As Button
    Private ReadOnly lblMessage As Label = UiKit.MakeLabel(String.Empty)

    Public Sub New(forced As Boolean)
        Text = ErrorHandler.APP_TITLE & " - Change password"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterScreen
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(420, 420)
        btnCancel = UiKit.MakeButton(If(forced, "Log out", "Cancel"), False)
        If forced Then ControlBox = False

        Controls.Add(UiKit.MakeHeader("Change password", If(forced, "You must set a new password to continue.", "Choose a new password."), 420, 80))
        UiKit.PlaceField(Me, "Current password", 28, 100, txtCurrent, 28, 122, 364)
        UiKit.PlaceField(Me, "New password", 28, 158, txtNew, 28, 180, 364)
        UiKit.PlaceField(Me, "Confirm new password", 28, 216, txtConfirm, 28, 238, 364)

        Dim hint As Label = UiKit.MakeLabel("At least " & PasswordPolicy.MIN_LENGTH & " characters with letters and digits.")
        hint.Font = Theme.SmallFont
        hint.ForeColor = Theme.Muted
        hint.Location = New Point(28, 274)
        Controls.Add(hint)

        lblMessage.ForeColor = Theme.Danger
        lblMessage.AutoSize = False
        lblMessage.SetBounds(28, 298, 364, 40)
        Controls.Add(lblMessage)

        btnOk.Location = New Point(162, 358)
        btnCancel.Location = New Point(282, 358)
        Controls.Add(btnOk)
        Controls.Add(btnCancel)

        AcceptButton = btnOk
        CancelButton = btnCancel
        btnCancel.DialogResult = DialogResult.Cancel
        UiKit.AttachFocusColor(Me)
        AddHandler btnOk.Click, AddressOf OnOkClick
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        txtCurrent.Focus()
    End Sub

    Private Sub OnOkClick(sender As Object, e As EventArgs)
        lblMessage.Text = String.Empty
        If txtNew.Text <> txtConfirm.Text Then
            lblMessage.Text = "New password and confirmation do not match."
            txtConfirm.Clear()
            txtConfirm.Focus()
            Return
        End If
        btnOk.Enabled = False
        UseWaitCursor = True
        Try
            AppServices.Auth.ChangePassword(txtCurrent.Text, txtNew.Text)
            DialogResult = DialogResult.OK
        Catch ex As BusinessException
            lblMessage.Text = ex.Message
        Catch ex As Exception
            ShowError(ex)
        Finally
            UseWaitCursor = False
            btnOk.Enabled = True
        End Try
    End Sub
End Class
