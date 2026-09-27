Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Common
Imports HRMS.Models

''' <summary>Employee details in four tabs. Aadhaar, PAN, bank a/c, UAN and ESI number are encrypted by the BLL;
''' a blank box means "keep what is stored". Reveal (needs EMPLOYEE_SENSITIVE) is written to the audit log.</summary>
Friend Class frmEmployeeEdit
    Inherits BaseForm

    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _canSensitive As Boolean
    Private _id As Integer?

    Private ReadOnly tabs As New TabControl()
    Private ReadOnly tabNominees As New TabPage("Nominees")
    ' personal
    Private ReadOnly txtCode As TextBox = UiKit.MakeText()
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly txtFather As TextBox = UiKit.MakeText()
    Private ReadOnly dtpDob As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly cmbGender As New ComboBox()
    Private ReadOnly txtMobile As TextBox = UiKit.MakeText()
    Private ReadOnly txtAddress As TextBox = UiKit.MakeText()
    ' job
    Private ReadOnly dtpDoj As DateTimePicker = UiKit.MakeDate()
    Private ReadOnly cmbFactory As New ComboBox()
    Private ReadOnly cmbDept As New ComboBox()
    Private ReadOnly cmbDesig As New ComboBox()
    Private ReadOnly cmbSkill As New ComboBox()
    Private ReadOnly lblLeft As Label = UiKit.MakeLabel(String.Empty)
    ' ids and bank
    Private ReadOnly txtIfsc As TextBox = UiKit.MakeText()
    Private ReadOnly txtAadhaar As TextBox = UiKit.MakeText()
    Private ReadOnly txtPan As TextBox = UiKit.MakeText()
    Private ReadOnly txtBank As TextBox = UiKit.MakeText()
    Private ReadOnly txtUan As TextBox = UiKit.MakeText()
    Private ReadOnly txtEsi As TextBox = UiKit.MakeText()
    Private ReadOnly lblAadhaar As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly lblPan As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly lblBank As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly lblUan As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly lblEsi As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly btnReveal As Button = UiKit.MakeButton("Reveal values", False)
    Private ReadOnly nominees As ucEmployeeNominees
    Private ReadOnly more As New ucEmployeeMore()
    Private ReadOnly statutory As New ucEmployeeStatutory()
    ' bottom
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save (Ctrl+S)", True)
    Private ReadOnly btnClose As Button = UiKit.MakeButton("Close", False)
    Private ReadOnly lblStatus As Label = UiKit.MakeLabel(String.Empty)

    ''' <summary>The saved employee's ID (Nothing if nothing was saved).</summary>
    Public ReadOnly Property EmployeeID As Integer?
        Get
            Return _id
        End Get
    End Property

    Public Sub New(employeeID As Integer?)
        _id = employeeID
        Dim s As UserSession = AppSession.Require()
        _canEdit = s.Has("EMPLOYEE_EDIT")
        _canSensitive = s.Has("EMPLOYEE_SENSITIVE")
        Text = If(employeeID.HasValue, "Employee", "New employee")
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(760, 610)
        EnterMovesFocus = True

        tabs.SetBounds(12, 12, 736, 500)
        tabs.Controls.Add(BuildPersonalTab())
        tabs.Controls.Add(BuildJobTab())
        tabs.Controls.Add(BuildIdsTab())
        tabs.Controls.Add(WrapTab("Statutory", statutory))
        tabs.Controls.Add(WrapTab("More details", more))
        nominees = New ucEmployeeNominees(Function() _id, _canEdit)
        nominees.Dock = DockStyle.Fill
        tabNominees.Controls.Add(nominees)
        tabs.Controls.Add(tabNominees)
        Controls.Add(tabs)

        lblStatus.AutoSize = False
        lblStatus.ForeColor = Theme.Muted
        lblStatus.SetBounds(16, 522, 728, 22)
        Controls.Add(lblStatus)
        btnSave.Size = New Size(140, 34)
        btnSave.Location = New Point(468, 558)
        btnClose.Size = New Size(120, 34)
        btnClose.Location = New Point(618, 558)
        Controls.Add(btnSave)
        Controls.Add(btnClose)
        CancelButton = btnClose
        btnClose.DialogResult = DialogResult.Cancel

        UiKit.AttachFocusColor(Me)
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnReveal.Click, AddressOf OnRevealClick

        If employeeID.HasValue Then
            LoadEmployee()
        Else
            LoadLookups(Nothing, Nothing, Nothing)
            lblLeft.Text = "Working"
            txtCode.Text = "(assigned on save)"
            SetPresence(Nothing)
            more.LoadFrom(New EmployeeDetail())
            statutory.LoadFrom(New EmployeeDetail())
        End If
        ApplyPermissions()
        tabNominees.Enabled = _id.HasValue
    End Sub

    ' ---------- tabs ----------
    Private Shared Function WrapTab(title As String, content As Control) As TabPage
        Dim t As New TabPage(title)
        t.BackColor = Color.White
        content.Dock = DockStyle.Fill
        t.Controls.Add(content)
        Return t
    End Function

    Private Function BuildPersonalTab() As TabPage
        Dim t As New TabPage("Personal")
        t.BackColor = Color.White
        txtCode.ReadOnly = True
        txtCode.TabStop = False
        txtName.MaxLength = 100
        txtFather.MaxLength = 100
        txtMobile.MaxLength = 10
        txtAddress.MaxLength = 300
        txtAddress.Multiline = True
        txtAddress.Height = 70
        cmbGender.DropDownStyle = ComboBoxStyle.DropDownList
        cmbGender.Items.AddRange(New Object() {"(not set)", "Male", "Female", "Other"})
        cmbGender.SelectedIndex = 0
        UiKit.PlaceField(t, "Employee code", 16, 20, txtCode, 200, 16, 180)
        UiKit.PlaceField(t, "Name *", 16, 60, txtName, 200, 56, 360)
        UiKit.PlaceField(t, "Father / husband name", 16, 100, txtFather, 200, 96, 360)
        UiKit.PlaceField(t, "Date of birth", 16, 140, dtpDob, 200, 136, 160)
        UiKit.PlaceField(t, "Gender", 16, 180, cmbGender, 200, 176, 160)
        UiKit.PlaceField(t, "Mobile (10 digits)", 16, 220, txtMobile, 200, 216, 160)
        UiKit.PlaceField(t, "Address", 16, 260, txtAddress, 200, 256, 460)
        Return t
    End Function

    Private Function BuildJobTab() As TabPage
        Dim t As New TabPage("Job")
        t.BackColor = Color.White
        cmbSkill.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSkill.Items.AddRange(New Object() {"(not set)", "Unskilled", "Semi-skilled", "Skilled", "Highly skilled", "Clerical"})
        cmbSkill.SelectedIndex = 0
        UiKit.PlaceField(t, "Date of joining *", 16, 20, dtpDoj, 200, 16, 160)
        UiKit.PlaceField(t, "Factory", 16, 60, cmbFactory, 200, 56, 300)
        UiKit.PlaceField(t, "Department", 16, 100, cmbDept, 200, 96, 300)
        UiKit.PlaceField(t, "Designation", 16, 140, cmbDesig, 200, 136, 300)
        UiKit.PlaceField(t, "Skill category", 16, 180, cmbSkill, 200, 176, 200)
        Dim cap As Label = UiKit.MakeLabel("Status")
        cap.Location = New Point(16, 224)
        t.Controls.Add(cap)
        lblLeft.Location = New Point(200, 224)
        t.Controls.Add(lblLeft)
        Dim note As Label = UiKit.MakeLabel("Use Exit employee on the list to record a leaving date.")
        note.ForeColor = Theme.Muted
        note.Font = Theme.SmallFont
        note.Location = New Point(200, 248)
        t.Controls.Add(note)
        Return t
    End Function

    Private Function BuildIdsTab() As TabPage
        Dim t As New TabPage("IDs and bank")
        t.BackColor = Color.White
        txtIfsc.MaxLength = 11
        txtIfsc.CharacterCasing = CharacterCasing.Upper
        txtPan.MaxLength = 10
        txtPan.CharacterCasing = CharacterCasing.Upper
        txtAadhaar.MaxLength = 12
        txtBank.MaxLength = 18
        txtUan.MaxLength = 12
        txtEsi.MaxLength = 17
        UiKit.PlaceField(t, "Bank IFSC", 16, 20, txtIfsc, 200, 16, 160)
        Dim sep As Label = UiKit.MakeLabel("Encrypted fields (leave a box empty to keep the stored value)")
        sep.Font = New Font(Theme.BaseFont, FontStyle.Bold)
        sep.ForeColor = Theme.Primary
        sep.Location = New Point(16, 64)
        t.Controls.Add(sep)
        UiKit.PlaceField(t, "Aadhaar (12 digits)", 16, 100, txtAadhaar, 200, 96, 200)
        UiKit.PlaceField(t, "PAN", 16, 140, txtPan, 200, 136, 200)
        UiKit.PlaceField(t, "Bank account", 16, 180, txtBank, 200, 176, 200)
        UiKit.PlaceField(t, "UAN (12 digits)", 16, 220, txtUan, 200, 216, 200)
        UiKit.PlaceField(t, "ESI IP number", 16, 260, txtEsi, 200, 256, 200)
        Dim y As Integer = 100
        For Each l As Label In New Label() {lblAadhaar, lblPan, lblBank, lblUan, lblEsi}
            l.Location = New Point(420, y + 2)
            t.Controls.Add(l)
            y += 40
        Next
        btnReveal.Size = New Size(150, 32)
        btnReveal.Location = New Point(200, 310)
        t.Controls.Add(btnReveal)
        Dim note As Label = UiKit.MakeLabel("Reveal shows the stored numbers and is written to the audit log.")
        note.Font = Theme.SmallFont
        note.ForeColor = Theme.Muted
        note.Location = New Point(200, 348)
        t.Controls.Add(note)
        Return t
    End Function

    ' ---------- load ----------
    ''' <summary>Lists show active items plus the one already assigned (even if it was deactivated later).</summary>
    Private Sub LoadLookups(factoryID As Integer?, deptID As Integer?, desigID As Integer?)
        Try
            UiKit.BindLookup(cmbFactory, UiKit.ActiveOrCurrent(AppServices.Masters.Factories(False), "FactoryID", factoryID), "FactoryID", "Name", "(none)")
            UiKit.BindLookup(cmbDept, UiKit.ActiveOrCurrent(AppServices.Masters.Departments(False), "DeptID", deptID), "DeptID", "Name", "(none)")
            UiKit.BindLookup(cmbDesig, UiKit.ActiveOrCurrent(AppServices.Masters.Designations(False), "DesigID", desigID), "DesigID", "Name", "(none)")
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub LoadEmployee()
        Try
            Dim rec As EmployeeRecord = AppServices.Employees.Load(_id.Value, False)
            LoadLookups(rec.FactoryID, rec.DeptID, rec.DesigID)
            txtCode.Text = rec.EmpCode
            txtName.Text = rec.Name
            txtFather.Text = rec.FatherName
            UiKit.SetDate(dtpDob, rec.DOB)
            cmbGender.SelectedIndex = GenderIndex(rec.Gender)
            txtMobile.Text = rec.Mobile
            txtAddress.Text = rec.Address
            dtpDoj.Value = rec.DOJ
            UiKit.SelectId(cmbFactory, rec.FactoryID)
            UiKit.SelectId(cmbDept, rec.DeptID)
            UiKit.SelectId(cmbDesig, rec.DesigID)
            SelectSkill(rec.SkillCategory)
            txtIfsc.Text = rec.IFSC
            lblLeft.Text = If(rec.DOL.HasValue, "Left on " & rec.DOL.Value.ToString("dd-MM-yyyy"), "Working")
            SetPresence(rec)
            Text = "Employee " & rec.EmpCode & " - " & rec.Name
            Dim detail As EmployeeDetail = AppServices.Employees.LoadDetail(_id.Value)
            more.LoadFrom(detail)
            statutory.LoadFrom(detail)
            nominees.LoadNominees()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub SetPresence(rec As EmployeeRecord)
        SetOnFile(lblAadhaar, rec IsNot Nothing AndAlso rec.HasAadhaar)
        SetOnFile(lblPan, rec IsNot Nothing AndAlso rec.HasPAN)
        SetOnFile(lblBank, rec IsNot Nothing AndAlso rec.HasBankAcc)
        SetOnFile(lblUan, rec IsNot Nothing AndAlso rec.HasUAN)
        SetOnFile(lblEsi, rec IsNot Nothing AndAlso rec.HasEsiIP)
    End Sub

    Private Shared Sub SetOnFile(l As Label, present As Boolean)
        l.Text = If(present, "On file", "Not on file")
        l.ForeColor = If(present, Theme.Accent, Theme.Muted)
    End Sub

    Private Sub ApplyPermissions()
        Dim sensitive As Boolean = _canEdit AndAlso _canSensitive
        For Each t As TextBox In New TextBox() {txtAadhaar, txtPan, txtBank, txtUan, txtEsi}
            t.Enabled = sensitive
        Next
        btnReveal.Enabled = _canSensitive AndAlso _id.HasValue
        If Not _canSensitive Then lblStatus.Text = "You do not have permission to view or change Aadhaar, PAN, bank, UAN and ESI numbers."
        If _canEdit Then Return
        btnSave.Enabled = False
        For Each t As TextBox In New TextBox() {txtName, txtFather, txtMobile, txtAddress, txtIfsc}
            t.ReadOnly = True
        Next
        For Each c As Control In New Control() {dtpDob, cmbGender, dtpDoj, cmbFactory, cmbDept, cmbDesig, cmbSkill}
            c.Enabled = False
        Next
        more.SetReadOnly(True)
        statutory.SetReadOnly(True)
        lblStatus.Text = "View only."
    End Sub

    ' ---------- save ----------
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If e.Control AndAlso e.KeyCode = Keys.S Then
            e.Handled = True
            e.SuppressKeyPress = True
            If btnSave.Enabled Then SaveEmployee()
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        SaveEmployee()
    End Sub

    Private Sub SaveEmployee()
        Try
            Dim rec As New EmployeeRecord With {
                .EmployeeID = If(_id.HasValue, _id.Value, 0),
                .EmpCode = If(_id.HasValue, txtCode.Text, Nothing),
                .Name = txtName.Text.Trim(),
                .FatherName = UiKit.TextOrNothing(txtFather),
                .DOB = UiKit.DateValue(dtpDob),
                .Gender = GenderCode(cmbGender.SelectedIndex),
                .DOJ = dtpDoj.Value.Date,
                .FactoryID = UiKit.SelectedId(cmbFactory),
                .DeptID = UiKit.SelectedId(cmbDept),
                .DesigID = UiKit.SelectedId(cmbDesig),
                .SkillCategory = If(cmbSkill.SelectedIndex <= 0, Nothing, Convert.ToString(cmbSkill.SelectedItem)),
                .IFSC = UiKit.TextOrNothing(txtIfsc),
                .Mobile = UiKit.TextOrNothing(txtMobile),
                .Address = UiKit.TextOrNothing(txtAddress)}
            Dim secrets As New EmployeeSecrets With {
                .Aadhaar = txtAadhaar.Text, .PAN = txtPan.Text, .BankAcc = txtBank.Text, .UAN = txtUan.Text, .EsiIP = txtEsi.Text}
            ' check the detail tabs first so a bad value never leaves a half-saved employee
            Dim detail As New EmployeeDetail()
            more.ReadInto(detail)
            statutory.ReadInto(detail)
            Dim detailProblems = EmployeeService.ValidateDetail(detail)
            If detailProblems.Count > 0 Then Throw New BusinessException(String.Join(Environment.NewLine, detailProblems))

            Dim wasNew As Boolean = Not _id.HasValue
            _id = AppServices.Employees.Save(rec, secrets)
            If wasNew Then txtCode.Text = rec.EmpCode
            AppServices.Employees.SaveDetail(_id.Value, detail)

            Dim fresh As EmployeeRecord = AppServices.Employees.Load(_id.Value, False)
            SetPresence(fresh)
            Text = "Employee " & fresh.EmpCode & " - " & fresh.Name
            tabNominees.Enabled = True
            btnReveal.Enabled = _canSensitive
            lblStatus.ForeColor = Theme.Accent
            lblStatus.Text = "Saved " & fresh.EmpCode & If(wasNew, ". You can add nominees now.", ".")
            If wasNew Then nominees.LoadNominees()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnRevealClick(sender As Object, e As EventArgs)
        If Not _id.HasValue Then Return
        If Not Confirm("Showing the full numbers is written to the audit log. Continue?") Then Return
        Try
            Dim full As EmployeeRecord = AppServices.Employees.Load(_id.Value, True)
            If Not full.SensitiveVisible Then
                ShowInfo("You do not have permission to view these numbers.")
                Return
            End If
            Dim s As EmployeeSecrets = AppServices.Employees.ReadSecrets(full)
            txtAadhaar.Text = s.Aadhaar
            txtPan.Text = s.PAN
            txtBank.Text = s.BankAcc
            txtUan.Text = s.UAN
            txtEsi.Text = s.EsiIP
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    ' ---------- small helpers ----------
    Private Shared Function GenderIndex(code As String) As Integer
        Select Case code
            Case "M" : Return 1
            Case "F" : Return 2
            Case "O" : Return 3
            Case Else : Return 0
        End Select
    End Function

    Private Shared Function GenderCode(index As Integer) As String
        Select Case index
            Case 1 : Return "M"
            Case 2 : Return "F"
            Case 3 : Return "O"
            Case Else : Return Nothing
        End Select
    End Function

    Private Sub SelectSkill(value As String)
        If String.IsNullOrEmpty(value) Then
            cmbSkill.SelectedIndex = 0
            Return
        End If
        Dim idx As Integer = cmbSkill.Items.IndexOf(value)
        If idx < 0 Then idx = cmbSkill.Items.Add(value)
        cmbSkill.SelectedIndex = idx
    End Sub
End Class
