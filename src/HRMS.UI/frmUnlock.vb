Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.BLL

''' <summary>Full-screen cover shown after inactivity or Ctrl+L. OK = unlocked, Abort = log out.</summary>
Friend Class frmUnlock
    Inherits BaseForm

    Private ReadOnly pnlBox As New Panel()
    Private ReadOnly txtPass As TextBox = UiKit.MakeText(True)
    Private ReadOnly btnUnlock As Button = UiKit.MakeButton("Unlock", True)
    Private ReadOnly btnLogout As Button = UiKit.MakeButton("Log out", False)
    Private ReadOnly lblMessage As Label = UiKit.MakeLabel(String.Empty)

    Public Sub New(fullName As String, username As String)
        FormBorderStyle = FormBorderStyle.None
        StartPosition = FormStartPosition.Manual
        Bounds = SystemInformation.VirtualScreen
        TopMost = True
        ShowInTaskbar = False
        BackColor = Theme.Primary
        EnterMovesFocus = False

        pnlBox.BackColor = Color.White
        pnlBox.Size = New Size(380, 250)

        Dim title As Label = UiKit.MakeLabel("Session locked")
        title.Font = Theme.TitleFont
        title.ForeColor = Theme.Primary
        title.Location = New Point(24, 18)
        pnlBox.Controls.Add(title)

        Dim who As Label = UiKit.MakeLabel(fullName & " (" & username & ")")
        who.ForeColor = Theme.Muted
        who.Location = New Point(26, 60)
        pnlBox.Controls.Add(who)

        UiKit.PlaceField(pnlBox, "Password", 26, 92, txtPass, 26, 114, 328)

        lblMessage.ForeColor = Theme.Danger
        lblMessage.AutoSize = False
        lblMessage.SetBounds(26, 146, 328, 22)
        pnlBox.Controls.Add(lblMessage)

        btnUnlock.Location = New Point(26, 190)
        btnLogout.Location = New Point(244, 190)
        pnlBox.Controls.Add(btnUnlock)
        pnlBox.Controls.Add(btnLogout)
        Controls.Add(pnlBox)

        AcceptButton = btnUnlock
        UiKit.AttachFocusColor(pnlBox)
        AddHandler btnUnlock.Click, AddressOf OnUnlockClick
        AddHandler btnLogout.Click, AddressOf OnLogoutClick
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        CenterBox()
        Activate()
        txtPass.Focus()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        CenterBox()
    End Sub

    Private Sub CenterBox()
        pnlBox.Location = New Point((ClientSize.Width - pnlBox.Width) \ 2, (ClientSize.Height - pnlBox.Height) \ 2)
    End Sub

    ''' <summary>Alt+F4 must not dismiss the lock.</summary>
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If DialogResult <> DialogResult.OK AndAlso DialogResult <> DialogResult.Abort Then e.Cancel = True
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub OnUnlockClick(sender As Object, e As EventArgs)
        lblMessage.Text = String.Empty
        btnUnlock.Enabled = False
        UseWaitCursor = True
        Try
            Dim result As LoginOutcome = AppServices.Auth.VerifyUnlock(txtPass.Text)
            If result.IsSuccess Then
                DialogResult = DialogResult.OK
            ElseIf result.Status = LoginStatus.Locked OrElse result.Status = LoginStatus.Inactive Then
                MessageBox.Show(Me, result.Message, ErrorHandler.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                DialogResult = DialogResult.Abort
            Else
                lblMessage.Text = result.Message
                txtPass.Clear()
                txtPass.Focus()
            End If
        Catch ex As Exception
            ShowError(ex)
        Finally
            UseWaitCursor = False
            btnUnlock.Enabled = True
        End Try
    End Sub

    Private Sub OnLogoutClick(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Abort
    End Sub
End Class
