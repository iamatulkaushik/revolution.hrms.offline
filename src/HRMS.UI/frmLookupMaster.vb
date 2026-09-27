Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>One screen for simple name-only masters (Department, Designation).</summary>
Friend Class frmLookupMaster
    Inherits BaseForm

    Private ReadOnly _idColumn As String
    Private ReadOnly _list As Func(Of Boolean, DataTable)
    Private ReadOnly _save As Func(Of Integer?, String, Integer)
    Private ReadOnly _setActive As Action(Of Integer, Boolean)
    Private ReadOnly _editPermission As String

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly chkInactive As New CheckBox()
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly btnNew As Button = UiKit.MakeButton("New", False)
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save", True)
    Private ReadOnly btnToggle As Button = UiKit.MakeButton("Deactivate", False)
    Private _editingId As Integer?
    Private _editingActive As Boolean = True
    Private _loading As Boolean

    Public Sub New(title As String, itemLabel As String, idColumn As String,
                   list As Func(Of Boolean, DataTable), save As Func(Of Integer?, String, Integer),
                   setActive As Action(Of Integer, Boolean), editPermission As String)
        _idColumn = idColumn
        _list = list
        _save = save
        _setActive = setActive
        _editPermission = editPermission
        Text = title
        ClientSize = New Size(560, 520)
        Dim canEdit As Boolean = AppSession.Require().Has(_editPermission)

        UiKit.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Controls.Add(grid)

        Dim editor As New GroupBox()
        editor.Text = itemLabel
        editor.Dock = DockStyle.Bottom
        editor.Height = 120
        txtName.MaxLength = 80
        UiKit.PlaceField(editor, "Name *", 16, 34, txtName, 90, 30, 250)
        btnNew.Location = New Point(360, 24)
        btnSave.Location = New Point(360, 64)
        btnToggle.Location = New Point(240, 64)
        chkInactive.Text = "Show inactive"
        chkInactive.AutoSize = True
        chkInactive.Location = New Point(18, 72)
        editor.Controls.Add(btnNew)
        editor.Controls.Add(btnSave)
        editor.Controls.Add(btnToggle)
        editor.Controls.Add(chkInactive)
        Controls.Add(editor)

        Dim header As Label = UiKit.MakeLabel(title)
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
            grid.DataSource = _list(Not chkInactive.Checked)
            UiKit.ShapeColumn(grid, _idColumn, Nothing, False)
            UiKit.ShapeColumn(grid, "Name", "Name")
            UiKit.ShapeColumn(grid, "IsActive", "Active")
            If selectId.HasValue Then
                For Each row As DataGridViewRow In grid.Rows
                    If Convert.ToInt32(row.Cells(_idColumn).Value) = selectId.Value Then
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
        Dim id As Integer? = UiKit.SelectedRowId(grid, _idColumn)
        _editingId = id
        If Not id.HasValue Then
            txtName.Clear()
            btnToggle.Text = "Deactivate"
            _editingActive = True
            Return
        End If
        Dim r As DataRow = DirectCast(grid.CurrentRow.DataBoundItem, DataRowView).Row
        txtName.Text = Convert.ToString(r("Name"))
        _editingActive = Convert.ToBoolean(r("IsActive"))
        btnToggle.Text = If(_editingActive, "Deactivate", "Activate")
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
        btnToggle.Text = "Deactivate"
        txtName.Focus()
    End Sub

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        Try
            LoadGrid(_save(_editingId, txtName.Text))
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnToggleClick(sender As Object, e As EventArgs)
        If Not _editingId.HasValue Then Return
        Try
            _setActive(_editingId.Value, Not _editingActive)
            LoadGrid(_editingId)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
