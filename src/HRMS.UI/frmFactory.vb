Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common
Imports HRMS.Models

Friend Class frmFactory
    Inherits BaseForm

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly chkInactive As New CheckBox()
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly txtLicense As TextBox = UiKit.MakeText()
    Private ReadOnly txtZone As TextBox = UiKit.MakeText()
    Private ReadOnly txtAddress As TextBox = UiKit.MakeText()
    Private ReadOnly cmbDivision As New ComboBox()
    Private _divisions As DataTable
    Private ReadOnly btnNew As Button = UiKit.MakeButton("New", False)
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save", True)
    Private ReadOnly btnToggle As Button = UiKit.MakeButton("Deactivate", False)
    Private _editingId As Integer?
    Private _editingActive As Boolean = True
    Private _loading As Boolean

    Public Sub New()
        Text = "Factories"
        ClientSize = New Size(760, 560)
        Dim canEdit As Boolean = AppSession.Require().Has("FACTORY_EDIT")

        UiKit.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Controls.Add(grid)

        Dim editor As New GroupBox()
        editor.Text = "Factory details"
        editor.Dock = DockStyle.Bottom
        editor.Height = 264
        txtName.MaxLength = 150
        txtLicense.MaxLength = 40
        txtZone.MaxLength = 20
        txtAddress.MaxLength = 300
        UiKit.PlaceField(editor, "Name *", 16, 34, txtName, 140, 30, 380)
        UiKit.PlaceField(editor, "Licence no.", 16, 68, txtLicense, 140, 64, 220)
        UiKit.PlaceField(editor, "Wage zone", 16, 102, txtZone, 140, 98, 140)
        UiKit.PlaceField(editor, "Address", 16, 136, txtAddress, 140, 132, 380)
        UiKit.PlaceField(editor, "Division", 16, 170, cmbDivision, 140, 166, 240)
        btnNew.Location = New Point(560, 28)
        btnSave.Location = New Point(560, 68)
        btnToggle.Location = New Point(560, 108)
        editor.Controls.Add(btnNew)
        editor.Controls.Add(btnSave)
        editor.Controls.Add(btnToggle)
        chkInactive.Text = "Show inactive"
        chkInactive.AutoSize = True
        chkInactive.Location = New Point(560, 156)
        editor.Controls.Add(chkInactive)
        Controls.Add(editor)

        Dim header As Label = UiKit.MakeLabel("Factories")
        header.Font = Theme.TitleFont
        header.ForeColor = Theme.Primary
        header.AutoSize = False
        header.Dock = DockStyle.Top
        header.Height = 44
        header.Padding = New Padding(12, 8, 0, 0)
        Controls.Add(header)

        btnNew.Enabled = canEdit
        btnSave.Enabled = canEdit
        btnToggle.Enabled = canEdit
        UiKit.AttachFocusColor(Me)
        AddHandler grid.SelectionChanged, AddressOf OnRowChanged
        AddHandler chkInactive.CheckedChanged, AddressOf OnFilterChanged
        AddHandler btnNew.Click, AddressOf OnNewClick
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnToggle.Click, AddressOf OnToggleClick
        LoadGrid(Nothing)
    End Sub

    Private Sub LoadGrid(selectId As Integer?)
        _loading = True
        Try
            _divisions = AppServices.Masters.Divisions(False)
            grid.DataSource = AppServices.Masters.Factories(Not chkInactive.Checked)
            UiKit.ShapeColumn(grid, "FactoryID", Nothing, False)
            UiKit.ShapeColumn(grid, "DivisionID", Nothing, False)
            UiKit.ShapeColumn(grid, "Division", "Division")
            UiKit.ShapeColumn(grid, "Name", "Factory")
            UiKit.ShapeColumn(grid, "LicenseNo", "Licence no.")
            UiKit.ShapeColumn(grid, "Zone", "Wage zone")
            UiKit.ShapeColumn(grid, "Address", "Address")
            UiKit.ShapeColumn(grid, "IsActive", "Active")
            If selectId.HasValue Then
                For Each row As DataGridViewRow In grid.Rows
                    If Convert.ToInt32(row.Cells("FactoryID").Value) = selectId.Value Then
                        grid.CurrentCell = row.Cells("Name")
                        Exit For
                    End If
                Next
            End If
        Catch ex As Exception
            ShowError(ex)
        Finally
            _loading = False
        End Try
        FillEditor()
    End Sub

    Private Sub FillEditor()
        Dim id As Integer? = UiKit.SelectedRowId(grid, "FactoryID")
        _editingId = id
        If Not id.HasValue Then
            ClearEditor()
            Return
        End If
        Dim r As DataRow = DirectCast(grid.CurrentRow.DataBoundItem, DataRowView).Row
        Dim divId As Integer? = If(r.IsNull("DivisionID"), CType(Nothing, Integer?), Convert.ToInt32(r("DivisionID")))
        BindDivisions(divId)
        txtName.Text = Convert.ToString(r("Name"))
        txtLicense.Text = If(r.IsNull("LicenseNo"), String.Empty, Convert.ToString(r("LicenseNo")))
        txtZone.Text = If(r.IsNull("Zone"), String.Empty, Convert.ToString(r("Zone")))
        txtAddress.Text = If(r.IsNull("Address"), String.Empty, Convert.ToString(r("Address")))
        _editingActive = Convert.ToBoolean(r("IsActive"))
        btnToggle.Text = If(_editingActive, "Deactivate", "Activate")
    End Sub

    Private Sub BindDivisions(current As Integer?)
        If _divisions Is Nothing Then Return
        UiKit.BindLookup(cmbDivision, UiKit.ActiveOrCurrent(_divisions, "DivisionID", current), "DivisionID", "Name", "(none)")
        UiKit.SelectId(cmbDivision, current)
    End Sub

    Private Sub ClearEditor()
        BindDivisions(Nothing)
        txtName.Clear()
        txtLicense.Clear()
        txtZone.Clear()
        txtAddress.Clear()
        btnToggle.Text = "Deactivate"
        _editingActive = True
    End Sub

    Private Sub OnRowChanged(sender As Object, e As EventArgs)
        If Not _loading Then FillEditor()
    End Sub

    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        LoadGrid(_editingId)
    End Sub

    Private Sub OnNewClick(sender As Object, e As EventArgs)
        _loading = True
        grid.ClearSelection()
        _loading = False
        _editingId = Nothing
        ClearEditor()
        txtName.Focus()
    End Sub

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        Try
            Dim f As New FactoryInfo With {
                .FactoryID = _editingId, .Name = txtName.Text.Trim(), .LicenseNo = UiKit.TextOrNothing(txtLicense),
                .Zone = UiKit.TextOrNothing(txtZone), .Address = UiKit.TextOrNothing(txtAddress),
                .DivisionID = UiKit.SelectedId(cmbDivision)}
            Dim id As Integer = AppServices.Masters.SaveFactory(f)
            LoadGrid(id)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnToggleClick(sender As Object, e As EventArgs)
        If Not _editingId.HasValue Then Return
        Try
            AppServices.Masters.SetFactoryActive(_editingId.Value, Not _editingActive)
            LoadGrid(_editingId)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
