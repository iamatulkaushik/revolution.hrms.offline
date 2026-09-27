Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Models

''' <summary>Shift master (att.Shift): list + edit. F2 New, Ctrl+S Save, F5 Refresh, Esc Close - matches
''' the hotkey table in design.md. Built with plain WinForms controls (no UiKit dependency) since UiKit's
''' current API wasn't available when this was written; wrap these controls in UiKit's grid/lookup
''' helpers later if that turns out to be less code than what's here.</summary>
Public Class frmShift
    Inherits Form

    Private ReadOnly svc As New MasterService()
    Private dgvList As DataGridView
    Private txtName As TextBox
    Private dtpStart As DateTimePicker
    Private dtpEnd As DateTimePicker
    Private nudHours As NumericUpDown
    Private chkShowInactive As CheckBox
    Private btnNew As Button
    Private btnSave As Button
    Private btnToggleActive As Button
    Private lblStatus As Label
    Private currentShiftID As Integer?

    Public Sub New()
        BuildLayout()
        AddHandler Me.Load, AddressOf frmShift_Load
        AddHandler Me.KeyDown, AddressOf frmShift_KeyDown
        Me.KeyPreview = True
    End Sub

    Private Sub BuildLayout()
        Me.Text = "Shift Master"
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.Size = New Size(760, 480)
        Me.StartPosition = FormStartPosition.CenterParent

        dgvList = New DataGridView() With {
            .Dock = DockStyle.Left, .Width = 420, .ReadOnly = True, .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        AddHandler dgvList.SelectionChanged, AddressOf dgvList_SelectionChanged

        Dim panel As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .Padding = New Padding(12), .RowCount = 7}
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

        txtName = New TextBox() With {.Dock = DockStyle.Fill}
        dtpStart = New DateTimePicker() With {.Format = DateTimePickerFormat.Time, .ShowUpDown = True, .Dock = DockStyle.Fill}
        dtpEnd = New DateTimePicker() With {.Format = DateTimePickerFormat.Time, .ShowUpDown = True, .Dock = DockStyle.Fill}
        nudHours = New NumericUpDown() With {.DecimalPlaces = 2, .Increment = 0.25D, .Minimum = 0D, .Maximum = 24D, .Dock = DockStyle.Fill}
        chkShowInactive = New CheckBox() With {.Text = "Show inactive", .AutoSize = True}
        AddHandler chkShowInactive.CheckedChanged, AddressOf chkShowInactive_CheckedChanged

        btnNew = New Button() With {.Text = "New (F2)", .Width = 100}
        btnSave = New Button() With {.Text = "Save (Ctrl+S)", .Width = 100}
        btnToggleActive = New Button() With {.Text = "Activate/Deactivate", .Width = 150}
        AddHandler btnNew.Click, AddressOf btnNew_Click
        AddHandler btnSave.Click, AddressOf btnSave_Click
        AddHandler btnToggleActive.Click, AddressOf btnToggleActive_Click

        Dim buttonRow As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True}
        buttonRow.Controls.AddRange(New Control() {btnNew, btnSave, btnToggleActive})

        lblStatus = New Label() With {.Dock = DockStyle.Fill, .AutoSize = False, .Height = 40, .ForeColor = Color.DimGray}

        Dim r As Integer = 0
        panel.Controls.Add(New Label() With {.Text = "Name", .AutoSize = True}, 0, r) : panel.Controls.Add(txtName, 1, r) : r += 1
        panel.Controls.Add(New Label() With {.Text = "Start time", .AutoSize = True}, 0, r) : panel.Controls.Add(dtpStart, 1, r) : r += 1
        panel.Controls.Add(New Label() With {.Text = "End time", .AutoSize = True}, 0, r) : panel.Controls.Add(dtpEnd, 1, r) : r += 1
        panel.Controls.Add(New Label() With {.Text = "Shift hours", .AutoSize = True}, 0, r) : panel.Controls.Add(nudHours, 1, r) : r += 1
        panel.Controls.Add(chkShowInactive, 1, r) : r += 1
        panel.Controls.Add(buttonRow, 1, r) : r += 1
        panel.Controls.Add(lblStatus, 1, r) : r += 1

        Me.Controls.Add(panel)
        Me.Controls.Add(dgvList)
    End Sub

    Private Sub frmShift_Load(sender As Object, e As EventArgs)
        RefreshGrid()
        ClearForm()
    End Sub

    Private Sub frmShift_KeyDown(sender As Object, e As KeyEventArgs)
        Select Case True
            Case e.KeyCode = Keys.F2 : btnNew_Click(sender, e)
            Case e.Control AndAlso e.KeyCode = Keys.S : btnSave_Click(sender, e) : e.SuppressKeyPress = True
            Case e.KeyCode = Keys.F5 : RefreshGrid()
            Case e.KeyCode = Keys.Escape : Me.Close()
        End Select
    End Sub

    Private Sub chkShowInactive_CheckedChanged(sender As Object, e As EventArgs)
        RefreshGrid()
    End Sub

    Private Sub RefreshGrid()
        Try
            dgvList.DataSource = svc.Shifts(Not chkShowInactive.Checked)
            If dgvList.Columns.Contains("IsActive") Then dgvList.Columns("IsActive").Visible = chkShowInactive.Checked
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub dgvList_SelectionChanged(sender As Object, e As EventArgs)
        If dgvList.CurrentRow Is Nothing Then Return
        Dim row As DataRowView = TryCast(dgvList.CurrentRow.DataBoundItem, DataRowView)
        If row Is Nothing Then Return
        currentShiftID = Convert.ToInt32(row("ShiftID"))
        txtName.Text = Convert.ToString(row("Name"))
        dtpStart.Value = Date.Today.Add(CType(row("StartTime"), TimeSpan))
        dtpEnd.Value = Date.Today.Add(CType(row("EndTime"), TimeSpan))
        nudHours.Value = Convert.ToDecimal(row("ShiftHours"))
    End Sub

    Private Sub btnNew_Click(sender As Object, e As EventArgs)
        ClearForm()
    End Sub

    Private Sub ClearForm()
        currentShiftID = Nothing
        txtName.Text = String.Empty
        dtpStart.Value = Date.Today.AddHours(9)
        dtpEnd.Value = Date.Today.AddHours(17)
        nudHours.Value = 8D
        txtName.Focus()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        Try
            Dim sh As New ShiftInfo With {
                .ShiftID = currentShiftID, .Name = txtName.Text.Trim(),
                .StartTime = dtpStart.Value.TimeOfDay, .EndTime = dtpEnd.Value.TimeOfDay, .ShiftHours = nudHours.Value
            }
            currentShiftID = svc.SaveShift(sh)
            lblStatus.Text = "Saved."
            RefreshGrid()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub btnToggleActive_Click(sender As Object, e As EventArgs)
        If Not currentShiftID.HasValue Then Return
        Try
            Dim row As DataRowView = TryCast(dgvList.CurrentRow?.DataBoundItem, DataRowView)
            Dim isActive As Boolean = If(row IsNot Nothing, Convert.ToBoolean(row("IsActive")), True)
            svc.SetShiftActive(currentShiftID.Value, Not isActive)
            RefreshGrid()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub ShowError(ex As Exception)
        lblStatus.Text = ex.Message
        MessageBox.Show(Me, ex.Message, "Shift", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub
End Class
