Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>Financial year setup: list, add/edit, set current.</summary>
Friend Class frmFinancialYear
    Inherits BaseForm

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly chkInactive As New CheckBox()
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly dtFrom As New DateTimePicker()
    Private ReadOnly dtTo As New DateTimePicker()
    Private ReadOnly btnNew As Button = UiKit.MakeButton("New", False)
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save", True)
    Private ReadOnly btnSetCurrent As Button = UiKit.MakeButton("Set as Current", False)
    Private _editingId As Integer?
    Private _loading As Boolean

    Public Sub New()
        Text = "Financial Year"
        ClientSize = New Size(940, 520)
        Dim canEdit As Boolean = AppSession.Require().Has("COMPANY_EDIT")
        Controls.Add(UiKit.MakeHeader("Financial Year", "Create or select Financial Year", ClientSize.Width, 80))

        UiKit.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        grid.Location = New Point(12, 80)
        Controls.Add(grid)

        Dim editor As New GroupBox()
        editor.Text = "Financial Year"
        editor.Dock = DockStyle.Bottom
        editor.Height = 150
        txtName.MaxLength = 9
        dtFrom.Format = DateTimePickerFormat.Short
        dtTo.Format = DateTimePickerFormat.Short
        UiKit.PlaceField(editor, "FY Name * (e.g. 2026-27)", 16, 34, txtName, 210, 30, 150)
        UiKit.PlaceField(editor, "From Date *", 16, 70, dtFrom, 210, 70, 150)
        UiKit.PlaceField(editor, "To Date *", 16, 106, dtTo, 210, 106, 150)
        btnNew.Location = New Point(400, 24)
        btnSave.Location = New Point(400, 64)
        btnSetCurrent.Location = New Point(400, 104)
        chkInactive.Text = "Show inactive"
        chkInactive.AutoSize = True
        chkInactive.Location = New Point(550, 24)
        editor.Controls.Add(btnNew)
        editor.Controls.Add(btnSave)
        editor.Controls.Add(btnSetCurrent)
        editor.Controls.Add(chkInactive)
        Controls.Add(editor)

        Dim header As Label = UiKit.MakeLabel("Financial Year")
        header.Font = Theme.TitleFont
        header.ForeColor = Theme.Primary
        header.AutoSize = False
        header.Dock = DockStyle.Top
        header.Height = 44
        header.Padding = New Padding(12, 8, 0, 0)
        header.Location = New Point(20, 10)
        Controls.Add(header)

        btnNew.Enabled = canEdit
        btnSave.Enabled = canEdit
        btnSetCurrent.Enabled = canEdit
        UiKit.AttachFocusColor(Me)
        AddHandler grid.SelectionChanged, AddressOf OnRowChanged
        AddHandler chkInactive.CheckedChanged, AddressOf OnFilterChanged
        AddHandler btnNew.Click, AddressOf OnNewClick
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnSetCurrent.Click, AddressOf OnSetCurrentClick
        LoadGrid(Nothing)
    End Sub

    Private Sub LoadGrid(selectId As Integer?)
        _loading = True
        Try
            grid.DataSource = AppServices.Config.FinancialYears(Not chkInactive.Checked)
            UiKit.ShapeColumn(grid, "FYID", Nothing, False)
            UiKit.ShapeColumn(grid, "FYName", "Financial Year")
            UiKit.ShapeColumn(grid, "FromDate", "From")
            UiKit.ShapeColumn(grid, "ToDate", "To")
            UiKit.ShapeColumn(grid, "IsCurrent", "Current")
            UiKit.ShapeColumn(grid, "IsActive", "Active")
            If selectId.HasValue Then
                For Each row As DataGridViewRow In grid.Rows
                    If Convert.ToInt32(row.Cells("FYID").Value) = selectId.Value Then
                        grid.CurrentCell = row.Cells("FYName")
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
        Dim id As Integer? = UiKit.SelectedRowId(grid, "FYID")
        _editingId = id
        If Not id.HasValue Then
            txtName.Clear()
            dtFrom.Value = Date.Today
            dtTo.Value = Date.Today.AddYears(1)
            btnSetCurrent.Enabled = False
            Return
        End If
        Dim r As DataRow = DirectCast(grid.CurrentRow.DataBoundItem, DataRowView).Row
        txtName.Text = Convert.ToString(r("FYName"))
        dtFrom.Value = Convert.ToDateTime(r("FromDate"))
        dtTo.Value = Convert.ToDateTime(r("ToDate"))
        btnSetCurrent.Enabled = AppSession.Require().Has("COMPANY_EDIT") AndAlso Not Convert.ToBoolean(r("IsCurrent"))
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
        txtName.Clear()
        dtFrom.Value = Date.Today
        dtTo.Value = Date.Today.AddYears(1)
        btnSetCurrent.Enabled = False
        txtName.Focus()
    End Sub

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        Try
            LoadGrid(AppServices.Config.SaveFinancialYear(_editingId, txtName.Text, dtFrom.Value.Date, dtTo.Value.Date))
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnSetCurrentClick(sender As Object, e As EventArgs)
        If Not _editingId.HasValue Then Return
        Try
            AppServices.Config.SetCurrentFinancialYear(_editingId.Value)
            LoadGrid(_editingId)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
