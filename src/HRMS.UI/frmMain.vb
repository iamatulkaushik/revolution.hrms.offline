Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.Reflection
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>MDI shell: role-based menu, status bar, idle lock.</summary>
Friend Class frmMain
    Inherits BaseForm

    Private Const IDLE_MINUTES As Integer = 10
    Private Const IDLE_CHECK_MS As Integer = 15000

    Private ReadOnly _menu As New MenuStrip()
    Private ReadOnly _status As New StatusStrip()
    Private ReadOnly _lblUser As New ToolStripStatusLabel()
    Private ReadOnly _lblCompany As New ToolStripStatusLabel()
    Private ReadOnly _lblFy As New ToolStripStatusLabel()
    Private ReadOnly _lblServer As New ToolStripStatusLabel()
    Private ReadOnly _idleTimer As New Timer()
    Private ReadOnly _activity As New ActivityFilter()
    Private _locked As Boolean
    Private _closingConfirmed As Boolean

    ''' <summary>True when the user chose Log out (or the lock screen logged them out): show the sign-in again.</summary>
    Public Property LogoutRequested As Boolean

    Public Sub New()
        IsMdiContainer = True
        WindowState = FormWindowState.Maximized
        MainMenuStrip = _menu
        BuildMenu()
        BuildStatusBar()
        Controls.Add(_menu)
        Controls.Add(_status)
        AddHandler AppServices.SessionChanged, AddressOf OnSessionChanged
        _idleTimer.Interval = IDLE_CHECK_MS
        AddHandler _idleTimer.Tick, AddressOf OnIdleTick
        RefreshStatus()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        _activity.Touch()
        Application.AddMessageFilter(_activity)
        _idleTimer.Start()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not _closingConfirmed AndAlso e.CloseReason = CloseReason.UserClosing Then
            If Not Confirm("Exit Revolution HRMS?") Then
                e.Cancel = True
                Return
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _idleTimer.Stop()
        RemoveHandler AppServices.SessionChanged, AddressOf OnSessionChanged
        Application.RemoveMessageFilter(_activity)
        MyBase.OnFormClosed(e)
    End Sub

    ' ---------- menu ----------
    Private Sub BuildMenu()
        Dim session As UserSession = AppSession.Require()
        Dim file As New ToolStripMenuItem("&File")

        Dim miPwd As New ToolStripMenuItem("Change &password...")
        AddHandler miPwd.Click, AddressOf OnChangePassword
        file.DropDownItems.Add(miPwd)

        If session.UserType = "Associate" Then
            Dim miSwitch As New ToolStripMenuItem("&Switch company...")
            AddHandler miSwitch.Click, AddressOf OnSwitchCompany
            file.DropDownItems.Add(miSwitch)
        End If

        Dim miLock As New ToolStripMenuItem("&Lock")
        miLock.ShortcutKeys = Keys.Control Or Keys.L
        AddHandler miLock.Click, AddressOf OnLockClick
        file.DropDownItems.Add(miLock)

        file.DropDownItems.Add(New ToolStripSeparator())
        Dim miLogout As New ToolStripMenuItem("Log &out")
        AddHandler miLogout.Click, AddressOf OnLogoutClick
        file.DropDownItems.Add(miLogout)
        Dim miExit As New ToolStripMenuItem("E&xit")
        AddHandler miExit.Click, AddressOf OnExitClick
        file.DropDownItems.Add(miExit)
        _menu.Items.Add(file)

        MenuCatalog.AppendModules(_menu, session, AddressOf OpenModule)

        Dim help As New ToolStripMenuItem("&Help")
        Dim miAbout As New ToolStripMenuItem("&About")
        AddHandler miAbout.Click, AddressOf OnAbout
        help.DropDownItems.Add(miAbout)
        _menu.Items.Add(help)
    End Sub

    Private Sub OpenModule(entry As MenuEntry)
        Try
            Dim session As UserSession = AppSession.Require()
            If Not session.CompanyID.HasValue AndAlso entry.Key <> "mst.company" AndAlso Not entry.Key.StartsWith("adm.", StringComparison.Ordinal) Then
                ShowInfo("Select or create a company first (Masters > Company, or File > Switch company).")
                Return
            End If
            For Each child As Form In MdiChildren
                If Object.Equals(child.Tag, entry.Key) Then
                    child.Activate()
                    Return
                End If
            Next
            Dim frm As Form = Nothing
            If FormRegistry.TryCreate(entry.Key, frm) Then
                frm.MdiParent = Me
                frm.Tag = entry.Key
                frm.Show()
            Else
                ShowInfo(entry.Caption & " is planned for " & entry.Phase & ".")
            End If
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    ' ---------- status bar ----------
    Private Sub BuildStatusBar()
        For Each item As ToolStripStatusLabel In New ToolStripStatusLabel() {_lblUser, _lblCompany, _lblFy, _lblServer}
            item.BorderSides = ToolStripStatusLabelBorderSides.Right
            item.Padding = New Padding(6, 0, 6, 0)
            _status.Items.Add(item)
        Next
        _lblServer.Spring = True
        _lblServer.TextAlign = Drawing.ContentAlignment.MiddleLeft
        _lblServer.BorderSides = ToolStripStatusLabelBorderSides.None
    End Sub

    Private Sub RefreshStatus()
        Dim s As UserSession = AppSession.Require()
        _lblUser.Text = s.FullName & " (" & s.UserType & ")"
        _lblCompany.Text = "Company: " & If(s.CompanyName, "(none)")
        _lblFy.Text = "FY " & FinancialYear.FyName(Date.Today)
        _lblServer.Text = "Server: " & AppServices.ServerName
        Text = ErrorHandler.APP_TITLE & If(s.CompanyName Is Nothing, String.Empty, " - " & s.CompanyName)
    End Sub

    ' ---------- file menu actions ----------
    Private Sub OnChangePassword(sender As Object, e As EventArgs)
        Using f As New frmChangePassword(False)
            If f.ShowDialog(Me) = DialogResult.OK Then ShowInfo("Password changed.")
        End Using
    End Sub

    Private Sub OnSwitchCompany(sender As Object, e As EventArgs)
        Try
            Using f As New frmCompanySelect(AppServices.Auth.GetCompanies(), True, "Cancel")
                If f.ShowDialog(Me) <> DialogResult.OK Then Return
                If Not CloseAllChildren() Then Return
                AppServices.Auth.CompleteLogin(f.SelectedCompanyID)
            End Using
            RefreshStatus()
        Catch ex As Exception
            ShowError(ex)
            RefreshStatus()
        End Try
    End Sub

    ''' <summary>Company changed elsewhere (new company created): refresh and drop screens showing the old company.</summary>
    Private Sub OnSessionChanged(sender As Object, e As EventArgs)
        For Each child As Form In MdiChildren
            If Not Object.Equals(child.Tag, "mst.company") Then child.Close()
        Next
        RefreshStatus()
    End Sub

    ''' <summary>Data of the old company must not stay on screen after a switch.</summary>
    Private Function CloseAllChildren() As Boolean
        For Each child As Form In MdiChildren
            child.Close()
        Next
        Return MdiChildren.Length = 0
    End Function

    Private Sub OnLockClick(sender As Object, e As EventArgs)
        LockScreen()
    End Sub

    Private Sub OnLogoutClick(sender As Object, e As EventArgs)
        If Not Confirm("Log out?") Then Return
        LogoutRequested = True
        _closingConfirmed = True
        Close()
    End Sub

    Private Sub OnExitClick(sender As Object, e As EventArgs)
        Close()
    End Sub

    Private Sub OnAbout(sender As Object, e As EventArgs)
        Dim v As Version = Assembly.GetExecutingAssembly().GetName().Version
        ShowInfo("Revolution HRMS" & Environment.NewLine & "Version " & v.ToString(3) & Environment.NewLine &
                 "Offline payroll and labour-law compliance (Haryana).")
    End Sub

    ' ---------- idle lock ----------
    Private Sub OnIdleTick(sender As Object, e As EventArgs)
        If _locked Then Return
        If _activity.IdleFor >= TimeSpan.FromMinutes(IDLE_MINUTES) Then LockScreen()
    End Sub

    Private Sub LockScreen()
        If _locked Then Return
        _locked = True
        _idleTimer.Stop()
        Dim s As UserSession = AppSession.Require()
        Dim result As DialogResult
        Using f As New frmUnlock(s.FullName, s.Username)
            result = f.ShowDialog()
        End Using
        _locked = False
        If result = DialogResult.OK Then
            _activity.Touch()
            _idleTimer.Start()
        Else
            LogoutRequested = True
            _closingConfirmed = True
            Close()
        End If
    End Sub
End Class
