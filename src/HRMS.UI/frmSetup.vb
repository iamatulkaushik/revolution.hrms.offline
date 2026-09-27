Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Common
Imports HRMS.Models

''' <summary>First-run wizard: 1) database connection, 2) encryption key, 3) first administrator.
''' Also reachable when the database or key is missing on this PC.</summary>
Friend Class frmSetup
    Inherits BaseForm

    Private ReadOnly _protector As IKeyProtector
    Private ReadOnly _keys As KeyStore
    Private ReadOnly _auth As New AuthService()
    Private _cs As String
    Private _dbOk As Boolean
    Private _adminNeeded As Boolean
    Private _adminDone As Boolean

    ' database
    Private ReadOnly txtServer As TextBox = UiKit.MakeText()
    Private ReadOnly txtDb As TextBox = UiKit.MakeText()
    Private ReadOnly txtDbUser As TextBox = UiKit.MakeText()
    Private ReadOnly txtDbPwd As TextBox = UiKit.MakeText(True)
    Private ReadOnly chkTrust As New CheckBox()
    Private ReadOnly btnTest As Button = UiKit.MakeButton("Test connection", False)
    Private ReadOnly lblDbResult As Label = UiKit.MakeLabel(String.Empty)
    ' key
    Private ReadOnly lblKey As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly btnCreateKey As Button = UiKit.MakeButton("Create new key", False)
    Private ReadOnly btnImportKey As Button = UiKit.MakeButton("Import key file", False)
    Private ReadOnly btnExportKey As Button = UiKit.MakeButton("Export key backup", False)
    ' admin
    Private ReadOnly gbAdmin As New GroupBox()
    Private ReadOnly txtAdminUser As TextBox = UiKit.MakeText()
    Private ReadOnly txtAdminName As TextBox = UiKit.MakeText()
    Private ReadOnly txtAdminPwd As TextBox = UiKit.MakeText(True)
    Private ReadOnly txtAdminPwd2 As TextBox = UiKit.MakeText(True)
    Private ReadOnly btnCreateAdmin As Button = UiKit.MakeButton("Create admin", False)
    Private ReadOnly lblAdminResult As Label = UiKit.MakeLabel(String.Empty)
    ' bottom
    Private ReadOnly lblStatus As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly btnFinish As Button = UiKit.MakeButton("Finish", True)
    Private ReadOnly btnExit As Button = UiKit.MakeButton("Exit", False)

    Public Sub New(protector As IKeyProtector, keys As KeyStore, problem As String)
        _protector = protector
        _keys = keys
        Text = ErrorHandler.APP_TITLE & " - Setup"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterScreen
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(640, 690)
        EnterMovesFocus = False

        Controls.Add(UiKit.MakeHeader("First-time setup", If(String.IsNullOrEmpty(problem), "Connect the database, protect the data, create the administrator.", problem), 640, 78))
        BuildDatabaseBox()
        BuildKeyBox()
        BuildAdminBox()

        lblStatus.ForeColor = Theme.Muted
        lblStatus.Location = New Point(16, 646)
        Controls.Add(lblStatus)
        btnFinish.Location = New Point(404, 640)
        btnExit.Location = New Point(520, 640)
        Controls.Add(btnFinish)
        Controls.Add(btnExit)
        CancelButton = btnExit
        btnExit.DialogResult = DialogResult.Cancel

        UiKit.AttachFocusColor(Me)
        AddHandler btnTest.Click, AddressOf OnTestClick
        AddHandler btnCreateKey.Click, AddressOf OnCreateKeyClick
        AddHandler btnImportKey.Click, AddressOf OnImportKeyClick
        AddHandler btnExportKey.Click, AddressOf OnExportKeyClick
        AddHandler btnCreateAdmin.Click, AddressOf OnCreateAdminClick
        AddHandler btnFinish.Click, AddressOf OnFinishClick
        For Each t As TextBox In New TextBox() {txtServer, txtDb, txtDbUser, txtDbPwd}
            AddHandler t.TextChanged, AddressOf OnDbFieldChanged
        Next
        AddHandler chkTrust.CheckedChanged, AddressOf OnDbFieldChanged

        RefreshKeyStatus()
        RefreshAdminBox()
        UpdateFinish()
    End Sub

    Private Sub BuildDatabaseBox()
        Dim gb As New GroupBox()
        gb.Text = "1. Database"
        gb.SetBounds(16, 90, 608, 216)
        txtServer.Text = ".\SQLEXPRESS"
        txtDb.Text = "HRMS_Data"
        txtDbUser.Text = "hrms_app"
        UiKit.PlaceField(gb, "Server", 16, 32, txtServer, 150, 28, 300)
        UiKit.PlaceField(gb, "Database", 16, 64, txtDb, 150, 60, 300)
        UiKit.PlaceField(gb, "Login", 16, 96, txtDbUser, 150, 92, 300)
        UiKit.PlaceField(gb, "Password", 16, 128, txtDbPwd, 150, 124, 300)
        chkTrust.Text = "Trust server certificate (self-signed, LAN)"
        chkTrust.Checked = True
        chkTrust.AutoSize = True
        chkTrust.Location = New Point(150, 156)
        gb.Controls.Add(chkTrust)
        btnTest.Size = New Size(130, 32)
        btnTest.Location = New Point(466, 26)
        gb.Controls.Add(btnTest)
        lblDbResult.AutoSize = False
        lblDbResult.SetBounds(16, 184, 576, 24)
        gb.Controls.Add(lblDbResult)
        Controls.Add(gb)
    End Sub

    Private Sub BuildKeyBox()
        Dim gb As New GroupBox()
        gb.Text = "2. Encryption key (Aadhaar, PAN, bank, UAN, ESI number)"
        gb.SetBounds(16, 316, 608, 128)
        lblKey.AutoSize = False
        lblKey.SetBounds(16, 26, 576, 38)
        gb.Controls.Add(lblKey)
        btnCreateKey.Size = New Size(140, 32)
        btnImportKey.Size = New Size(140, 32)
        btnExportKey.Size = New Size(160, 32)
        btnCreateKey.Location = New Point(16, 76)
        btnImportKey.Location = New Point(166, 76)
        btnExportKey.Location = New Point(316, 76)
        gb.Controls.Add(btnCreateKey)
        gb.Controls.Add(btnImportKey)
        gb.Controls.Add(btnExportKey)
        Controls.Add(gb)
    End Sub

    Private Sub BuildAdminBox()
        gbAdmin.Text = "3. Administrator account"
        gbAdmin.SetBounds(16, 454, 608, 176)
        txtAdminUser.Text = "admin"
        UiKit.PlaceField(gbAdmin, "Username", 16, 30, txtAdminUser, 170, 26, 260)
        UiKit.PlaceField(gbAdmin, "Full name", 16, 60, txtAdminName, 170, 56, 260)
        UiKit.PlaceField(gbAdmin, "Password", 16, 90, txtAdminPwd, 170, 86, 260)
        UiKit.PlaceField(gbAdmin, "Confirm password", 16, 120, txtAdminPwd2, 170, 116, 260)
        btnCreateAdmin.Size = New Size(130, 32)
        btnCreateAdmin.Location = New Point(466, 26)
        gbAdmin.Controls.Add(btnCreateAdmin)
        lblAdminResult.AutoSize = False
        lblAdminResult.SetBounds(16, 148, 576, 22)
        gbAdmin.Controls.Add(lblAdminResult)
        Controls.Add(gbAdmin)
    End Sub

    ' ---------- database ----------
    Private Sub OnDbFieldChanged(sender As Object, e As EventArgs)
        _dbOk = False
        _adminNeeded = False
        lblDbResult.Text = String.Empty
        RefreshAdminBox()
        UpdateFinish()
    End Sub

    Private Sub OnTestClick(sender As Object, e As EventArgs)
        UseWaitCursor = True
        Try
            Dim cs As String = SetupService.BuildConnectionString(txtServer.Text, txtDb.Text, txtDbUser.Text, txtDbPwd.Text, chkTrust.Checked)
            Dim probe As ConnectionProbe = SetupService.Probe(cs)
            SetupService.Configure(cs)
            _cs = cs
            _dbOk = True
            _adminNeeded = probe.AdminNeeded
            lblDbResult.ForeColor = Theme.Accent
            lblDbResult.Text = "Connected. SQL Server " & probe.ServerVersion
        Catch ex As BusinessException
            _dbOk = False
            lblDbResult.ForeColor = Theme.Danger
            lblDbResult.Text = ex.Message
        Catch ex As Exception
            _dbOk = False
            ShowError(ex)
        Finally
            UseWaitCursor = False
        End Try
        RefreshAdminBox()
        UpdateFinish()
    End Sub

    ' ---------- key ----------
    Private Sub RefreshKeyStatus()
        Dim exists As Boolean = _keys.Exists()
        lblKey.ForeColor = If(exists, Theme.Accent, Theme.Muted)
        lblKey.Text = If(exists,
            "A key is installed on this PC. Export a backup and keep it offline: without the key those fields cannot be read.",
            "No key on this PC. First PC: create a new key. Other PCs on the LAN: import the key file exported from the first PC.")
        btnCreateKey.Enabled = Not exists
        btnImportKey.Enabled = Not exists
        btnExportKey.Enabled = exists
    End Sub

    Private Sub OnCreateKeyClick(sender As Object, e As EventArgs)
        Try
            _keys.CreateNew()
            RefreshKeyStatus()
            UpdateFinish()
            If Confirm("Key created. Save a backup file now? (Strongly recommended)") Then ExportKey()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnImportKeyClick(sender As Object, e As EventArgs)
        Try
            Using dlg As New OpenFileDialog()
                dlg.Title = "Select the exported key file"
                dlg.Filter = "HRMS key file (*.hrk)|*.hrk|All files (*.*)|*.*"
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                Using pf As New frmPassphrase("Import key", False)
                    If pf.ShowDialog(Me) <> DialogResult.OK Then Return
                    _keys.ImportFromFile(dlg.FileName, pf.Passphrase)
                End Using
            End Using
            RefreshKeyStatus()
            UpdateFinish()
            ShowInfo("Key imported.")
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnExportKeyClick(sender As Object, e As EventArgs)
        ExportKey()
    End Sub

    Private Sub ExportKey()
        Try
            Using dlg As New SaveFileDialog()
                dlg.Title = "Save key backup"
                dlg.FileName = "hrms-field-key-backup.hrk"
                dlg.Filter = "HRMS key file (*.hrk)|*.hrk"
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                Using pf As New frmPassphrase("Protect key backup", True)
                    If pf.ShowDialog(Me) <> DialogResult.OK Then Return
                    _keys.ExportToFile(dlg.FileName, pf.Passphrase)
                End Using
                ShowInfo("Backup saved. Store the file and the passphrase in different safe places.")
            End Using
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    ' ---------- admin ----------
    Private Sub RefreshAdminBox()
        gbAdmin.Enabled = _dbOk AndAlso _adminNeeded AndAlso Not _adminDone
        If Not _dbOk Then
            lblAdminResult.ForeColor = Theme.Muted
            lblAdminResult.Text = "Test the database connection first."
        ElseIf _adminDone Then
            lblAdminResult.ForeColor = Theme.Accent
            lblAdminResult.Text = "Administrator created."
        ElseIf Not _adminNeeded Then
            lblAdminResult.ForeColor = Theme.Accent
            lblAdminResult.Text = "An administrator account already exists."
        Else
            lblAdminResult.Text = String.Empty
        End If
    End Sub

    Private Sub OnCreateAdminClick(sender As Object, e As EventArgs)
        If txtAdminPwd.Text <> txtAdminPwd2.Text Then
            lblAdminResult.ForeColor = Theme.Danger
            lblAdminResult.Text = "Passwords do not match."
            Return
        End If
        UseWaitCursor = True
        Try
            _auth.Bootstrap(txtAdminUser.Text, txtAdminName.Text, txtAdminPwd.Text)
            _adminDone = True
            txtAdminPwd.Clear()
            txtAdminPwd2.Clear()
        Catch ex As BusinessException
            lblAdminResult.ForeColor = Theme.Danger
            lblAdminResult.Text = ex.Message
        Catch ex As Exception
            ShowError(ex)
        Finally
            UseWaitCursor = False
        End Try
        RefreshAdminBox()
        UpdateFinish()
    End Sub

    ' ---------- finish ----------
    Private Sub UpdateFinish()
        Dim missing As String = Nothing
        If Not _dbOk Then
            missing = "Test the database connection."
        ElseIf Not _keys.Exists() Then
            missing = "Create or import the encryption key."
        ElseIf _adminNeeded AndAlso Not _adminDone Then
            missing = "Create the administrator account."
        End If
        btnFinish.Enabled = missing Is Nothing
        lblStatus.Text = If(missing, "Ready. Press Finish.")
    End Sub

    Private Sub OnFinishClick(sender As Object, e As EventArgs)
        Try
            SetupService.Save(_cs, _protector)
            DialogResult = DialogResult.OK
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
