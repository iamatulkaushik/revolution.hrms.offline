Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>Holiday calendar: list by year, add/edit, activate/deactivate.</summary>
Friend Class frmHolidayCalendar
    Inherits BaseForm

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly cmbYear As New ComboBox()
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly dtDate As New DateTimePicker()
    Private ReadOnly btnNew As Button = UiKit.MakeButton("New", False)
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save", True)
    Private ReadOnly btnToggle As Button = UiKit.MakeButton("Deactivate", False)
    Private _editingId As Integer?
    Private _editingActive As Boolean = True
    Private _loading As Boolean

    Public Sub New()
        Text = "Holiday Calendar"
        ClientSize = New Size(620, 520)
        Dim canEdit As Boolean = AppSession.Require().Has("ATT_EDIT")

        UiKit.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Controls.Add(grid)

        Dim editor As New GroupBox()
        editor.Text = "Holiday"
        editor.Dock = DockStyle.Bottom
        editor.Height = 120
        txtName.MaxLength = 80
        dtDate.Format = DateTimePickerFormat.Short
        cmbYear.DropDownStyle = ComboBoxStyle.DropDownList
        Dim thisYear As Integer = Date.Today.Year
        For y As Integer = thisYear - 1 To thisYear + 2
            cmbYear.Items.Add(y)
        Next
        cmbYear.SelectedItem = thisYear
        UiKit.PlaceField(editor, "Date *", 16, 34, dtDate, 60, 30, 150)
        UiKit.PlaceField(editor, "Name *", 240, 34, txtName, 60, 30, 200)
        Dim lblYear As Label = UiKit.MakeLabel("Year")
        lblYear.Location = New Point(16, 78)
        lblYear.AutoSize = True
        cmbYear.Location = New Point(76, 74)
        cmbYear.Width = 90
        editor.Controls.Add(lblYear)
        editor.Controls.Add(cmbYear)
        btnNew.Location = New Point(460, 24)
        btnSave.Location = New Point(460, 64)
        btnToggle.Location = New Point(340, 64)
        editor.Controls.Add(btnNew)
        editor.Controls.Add(btnSave)
        editor.Controls.Add(btnToggle)
        Controls.Add(editor)

        Dim header As Label = UiKit.MakeLabel("Holiday Calendar")
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
        AddHandler cmbYear.SelectedIndexChanged, AddressOf OnYearChanged
        AddHandler btnNew.Click, AddressOf OnNewClick
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnToggle.Click, AddressOf OnToggleClick
        LoadGrid(Nothing)
    End Sub

    Private Sub LoadGrid(selectId As Integer?)
        _loading = True
        Try
            Dim y As Integer? = If(cmbYear.SelectedItem Is Nothing, CType(Nothing, Integer?), CInt(cmbYear.SelectedItem))
            grid.DataSource = AppServices.Config.Holidays(y)
            UiKit.ShapeColumn(grid, "HolidayID", Nothing, False)
            UiKit.ShapeColumn(grid, "HolidayDate", "Date")
            UiKit.ShapeColumn(grid, "Name", "Holiday")
            UiKit.ShapeColumn(grid, "IsActive", "Active")
            If selectId.HasValue Then
                For Each row As DataGridViewRow In grid.Rows
                    If Convert.ToInt32(row.Cells("HolidayID").Value) = selectId.Value Then
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
        Dim id As Integer? = UiKit.SelectedRowId(grid, "HolidayID")
        _editingId = id
        If Not id.HasValue Then
            txtName.Clear()
            dtDate.Value = Date.Today
            btnToggle.Text = "Deactivate"
            _editingActive = True
            Return
        End If
        Dim r As DataRow = DirectCast(grid.CurrentRow.DataBoundItem, DataRowView).Row
        txtName.Text = Convert.ToString(r("Name"))
        dtDate.Value = Convert.ToDateTime(r("HolidayDate"))
        _editingActive = Convert.ToBoolean(r("IsActive"))
        btnToggle.Text = If(_editingActive, "Deactivate", "Activate")
    End Sub

    Private Sub OnRowChanged(sender As Object, e As EventArgs)
        If Not _loading Then FillEditor()
    End Sub

    Private Sub OnYearChanged(sender As Object, e As EventArgs)
        If Not _loading Then LoadGrid(Nothing)
    End Sub

    Private Sub OnNewClick(sender As Object, e As EventArgs)
        _loading = True
        grid.ClearSelection()
        _loading = False
        _editingId = Nothing
        txtName.Clear()
        dtDate.Value = Date.Today
        btnToggle.Text = "Deactivate"
        txtName.Focus()
    End Sub

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        Try
            LoadGrid(AppServices.Config.SaveHoliday(_editingId, dtDate.Value.Date, txtName.Text))
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnToggleClick(sender As Object, e As EventArgs)
        If Not _editingId.HasValue Then Return
        Try
            AppServices.Config.SetHolidayActive(_editingId.Value, Not _editingActive)
            LoadGrid(_editingId)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
