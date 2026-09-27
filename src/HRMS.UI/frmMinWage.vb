Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>Haryana minimum wage by zone/skill (FR-10). Effective-dated: each Save adds a new rate
''' and closes the previous open-ended one for the same zone/skill. No edit or deactivate - history only.</summary>
Friend Class frmMinWage
    Inherits BaseForm

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly cmbZone As New ComboBox()
    Private ReadOnly cmbSkill As New ComboBox()
    Private ReadOnly txtDaily As TextBox = UiKit.MakeText()
    Private ReadOnly txtMonthly As TextBox = UiKit.MakeText()
    Private ReadOnly dtFrom As New DateTimePicker()
    Private ReadOnly btnAdd As Button = UiKit.MakeButton("Add Rate", True)
    Private ReadOnly chkCurrentOnly As New CheckBox()

    Public Sub New()
        Text = "Minimum Wages (Haryana)"
        ClientSize = New Size(680, 520)
        Dim canEdit As Boolean = AppSession.Require().Has("RATE_EDIT")

        UiKit.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Controls.Add(grid)

        Dim editor As New GroupBox()
        editor.Text = "Add New Rate (effective from date; closes the prior open rate for this zone/skill)"
        editor.Dock = DockStyle.Bottom
        editor.Height = 120
        cmbZone.DropDownStyle = ComboBoxStyle.DropDownList
        cmbZone.Items.AddRange({"A", "B", "C"})
        cmbSkill.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSkill.Items.AddRange({"Unskilled", "Semi-Skilled", "Skilled", "Highly Skilled"})
        dtFrom.Format = DateTimePickerFormat.Short
        UiKit.PlaceField(editor, "Zone *", 16, 34, cmbZone, 60, 30, 90)
        UiKit.PlaceField(editor, "Skill *", 176, 34, cmbSkill, 60, 30, 150)
        UiKit.PlaceField(editor, "Daily Rate (₹) *", 16, 74, txtDaily, 100, 30, 110)
        UiKit.PlaceField(editor, "Monthly Rate (₹) *", 236, 74, txtMonthly, 110, 30, 110)
        UiKit.PlaceField(editor, "From Date *", 456, 34, dtFrom, 70, 30, 140)
        btnAdd.Location = New Point(456, 74)
        chkCurrentOnly.Text = "Current rates only"
        chkCurrentOnly.AutoSize = True
        chkCurrentOnly.Checked = True
        chkCurrentOnly.Location = New Point(590, 12)
        editor.Controls.Add(btnAdd)
        Controls.Add(editor)

        Dim header As Label = UiKit.MakeLabel("Minimum Wages (Haryana)")
        header.Font = Theme.TitleFont
        header.ForeColor = Theme.Primary
        header.AutoSize = False
        header.Dock = DockStyle.Top
        header.Height = 44
        header.Padding = New Padding(12, 8, 0, 0)
        Controls.Add(header)
        header.Controls.Add(chkCurrentOnly)

        btnAdd.Enabled = canEdit
        cmbZone.Enabled = canEdit
        cmbSkill.Enabled = canEdit
        txtDaily.Enabled = canEdit
        txtMonthly.Enabled = canEdit
        dtFrom.Enabled = canEdit
        UiKit.AttachFocusColor(Me)
        AddHandler chkCurrentOnly.CheckedChanged, Sub() LoadGrid()
        AddHandler btnAdd.Click, AddressOf OnAddClick
        LoadGrid()
    End Sub

    Private Sub LoadGrid()
        Try
            Dim asOf As Date? = If(chkCurrentOnly.Checked, CType(Date.Today, Date?), Nothing)
            grid.DataSource = AppServices.Masters.MinWages(asOfDate:=asOf)
            UiKit.ShapeColumn(grid, "MinWageID", Nothing, False)
            UiKit.ShapeColumn(grid, "Zone", "Zone")
            UiKit.ShapeColumn(grid, "SkillCategory", "Skill")
            UiKit.ShapeColumn(grid, "DailyRate", "Daily Rate")
            UiKit.ShapeColumn(grid, "MonthlyRate", "Monthly Rate")
            UiKit.ShapeColumn(grid, "FromDate", "From")
            UiKit.ShapeColumn(grid, "ToDate", "To")
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnAddClick(sender As Object, e As EventArgs)
        Try
            If cmbZone.SelectedItem Is Nothing Then Throw New BusinessException("Select a zone.")
            If cmbSkill.SelectedItem Is Nothing Then Throw New BusinessException("Select a skill category.")
            Dim daily As Decimal
            Dim monthly As Decimal
            If Not Decimal.TryParse(txtDaily.Text, daily) Then Throw New BusinessException("Enter a valid daily rate.")
            If Not Decimal.TryParse(txtMonthly.Text, monthly) Then Throw New BusinessException("Enter a valid monthly rate.")

            AppServices.Masters.SaveMinWage(CStr(cmbZone.SelectedItem), CStr(cmbSkill.SelectedItem), daily, monthly, dtFrom.Value.Date)
            txtDaily.Clear()
            txtMonthly.Clear()
            LoadGrid()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
