Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.BLL

Friend Class frmLogin
    Inherits BaseForm

    Private ReadOnly txtUser As TextBox = UiKit.MakeText()
    Private ReadOnly txtPass As TextBox = UiKit.MakeText(True)
    Private ReadOnly btnLogin As Button = UiKit.MakeButton("Sign in", True)
    Private ReadOnly btnExit As Button = UiKit.MakeButton("Exit", False)
    Private ReadOnly lblMessage As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly lblCaps As Label = UiKit.MakeLabel("Caps Lock is on")

    ''' <summary>Set when sign-in succeeds.</summary>
    Public Property Outcome As LoginOutcome

    Public Sub New(serverName As String)
        Text = ErrorHandler.APP_TITLE & " - Sign in"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterScreen
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(420, 350)

        Controls.Add(UiKit.MakeHeader("Revolution HRMS", "Sign in to continue", 420, 80))
        UiKit.PlaceField(Me, "Username", 28, 102, txtUser, 28, 124, 364)
        UiKit.PlaceField(Me, "Password", 28, 160, txtPass, 28, 182, 364)

        lblCaps.ForeColor = Theme.Danger
        lblCaps.Font = Theme.SmallFont
        lblCaps.Location = New Point(28, 212)
        lblCaps.Visible = False
        Controls.Add(lblCaps)

        lblMessage.ForeColor = Theme.Danger
        lblMessage.AutoSize = False
        lblMessage.SetBounds(28, 232, 364, 22)
        Controls.Add(lblMessage)

        btnLogin.Location = New Point(162, 268)
        btnExit.Location = New Point(282, 268)
        Controls.Add(btnLogin)
        Controls.Add(btnExit)

        Dim lblServer As Label = UiKit.MakeLabel("Server: " & serverName)
        lblServer.Font = Theme.SmallFont
        lblServer.ForeColor = Theme.Muted
        lblServer.Location = New Point(28, 318)
        Controls.Add(lblServer)

        AcceptButton = btnLogin
        CancelButton = btnExit
        btnExit.DialogResult = DialogResult.Cancel
        UiKit.AttachFocusColor(Me)

        AddHandler btnLogin.Click, AddressOf OnLoginClick
        AddHandler txtPass.KeyUp, AddressOf OnPasswordKeyUp
        AddHandler txtPass.Enter, AddressOf OnPasswordKeyUp
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        txtUser.Focus()
    End Sub

    Private Sub OnPasswordKeyUp(sender As Object, e As EventArgs)
        lblCaps.Visible = Control.IsKeyLocked(Keys.CapsLock)
    End Sub

    Private Sub OnLoginClick(sender As Object, e As EventArgs)
        lblMessage.Text = String.Empty
        btnLogin.Enabled = False
        UseWaitCursor = True
        Try
            Dim result As LoginOutcome = AppServices.Auth.Login(txtUser.Text, txtPass.Text)
            If result.IsSuccess Then
                Outcome = result
                DialogResult = DialogResult.OK
                Return
            End If
            lblMessage.Text = result.Message
            txtPass.Clear()
            txtPass.Focus()
        Catch ex As Exception
            ShowError(ex)
        Finally
            UseWaitCursor = False
            btnLogin.Enabled = True
        End Try
    End Sub
End Class
