Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Models

''' <summary>Nominee grid and editor for one employee. Only usable after the employee is saved.</summary>
Friend Class ucEmployeeNominees
    Inherits UserControl

    Private ReadOnly _getEmployeeId As Func(Of Integer?)
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly txtRelation As TextBox = UiKit.MakeText()
    Private ReadOnly dtpDob As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly nudShare As New NumericUpDown()
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Add / Update", True)
    Private ReadOnly btnDelete As Button = UiKit.MakeButton("Delete", False)
    Private ReadOnly btnClear As Button = UiKit.MakeButton("Clear", False)
    Private ReadOnly lblTotal As Label = UiKit.MakeLabel(String.Empty)
    Private _nomineeId As Integer?

    Public Sub New(getEmployeeId As Func(Of Integer?), canEdit As Boolean)
        _getEmployeeId = getEmployeeId
        BackColor = Color.White
        UiKit.StyleGrid(grid)
        grid.SetBounds(12, 12, 704, 190)
        Controls.Add(grid)
        txtName.MaxLength = 100
        txtRelation.MaxLength = 30
        nudShare.DecimalPlaces = 2
        nudShare.Minimum = 0.01D
        nudShare.Maximum = 100D
        nudShare.Value = 100D
        UiKit.PlaceField(Me, "Name *", 16, 218, txtName, 16, 240, 220)
        UiKit.PlaceField(Me, "Relation", 250, 218, txtRelation, 250, 240, 130)
        UiKit.PlaceField(Me, "Date of birth", 394, 218, dtpDob, 394, 240, 140)
        UiKit.PlaceField(Me, "Share %", 548, 218, nudShare, 548, 240, 90)
        btnSave.Size = New Size(130, 32)
        btnSave.Location = New Point(16, 286)
        btnDelete.Location = New Point(156, 286)
        btnClear.Location = New Point(276, 286)
        Controls.Add(btnSave)
        Controls.Add(btnDelete)
        Controls.Add(btnClear)
        lblTotal.Location = New Point(16, 334)
        Controls.Add(lblTotal)
        If Not canEdit Then
            txtName.ReadOnly = True
            txtRelation.ReadOnly = True
            dtpDob.Enabled = False
            nudShare.Enabled = False
            btnSave.Enabled = False
            btnDelete.Enabled = False
        End If
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnDelete.Click, AddressOf OnDeleteClick
        AddHandler btnClear.Click, AddressOf OnClearClick
        AddHandler grid.SelectionChanged, AddressOf OnRowChanged
    End Sub

    Public Sub LoadNominees()
        Dim empId As Integer? = _getEmployeeId()
        If Not empId.HasValue Then Return
        Try
            Dim dt As DataTable = AppServices.Employees.Nominees(empId.Value)
            grid.DataSource = dt
            UiKit.ShapeColumn(grid, "NomineeID", Nothing, False)
            UiKit.ShapeColumn(grid, "Name", "Name")
            UiKit.ShapeColumn(grid, "Relation", "Relation")
            UiKit.ShapeColumn(grid, "DOB", "Date of birth", True, "dd-MM-yyyy")
            UiKit.ShapeColumn(grid, "SharePct", "Share %", True, "0.00")
            Dim total As Decimal = 0D
            For Each r As DataRow In dt.Rows
                total += Convert.ToDecimal(r("SharePct"))
            Next
            lblTotal.Text = "Total share: " & total.ToString("0.00") & " %" & If(total = 100D, String.Empty, "  (should add up to 100)")
            lblTotal.ForeColor = If(total = 100D, Theme.Accent, Theme.Muted)
            ClearInputs()
        Catch ex As Exception
            ErrorHandler.Show(FindForm(), ex)
        End Try
    End Sub

    Private Sub ClearInputs()
        _nomineeId = Nothing
        txtName.Clear()
        txtRelation.Clear()
        dtpDob.Checked = False
        nudShare.Value = 100D
    End Sub

    Private Sub OnRowChanged(sender As Object, e As EventArgs)
        Dim id As Integer? = UiKit.SelectedRowId(grid, "NomineeID")
        If Not id.HasValue Then Return
        Dim r As DataRow = DirectCast(grid.CurrentRow.DataBoundItem, DataRowView).Row
        _nomineeId = id
        txtName.Text = Convert.ToString(r("Name"))
        txtRelation.Text = If(r.IsNull("Relation"), String.Empty, Convert.ToString(r("Relation")))
        UiKit.SetDate(dtpDob, If(r.IsNull("DOB"), CType(Nothing, Date?), Convert.ToDateTime(r("DOB"))))
        nudShare.Value = Convert.ToDecimal(r("SharePct"))
    End Sub

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        Dim empId As Integer? = _getEmployeeId()
        If Not empId.HasValue Then Return
        Try
            AppServices.Employees.SaveNominee(New NomineeInfo With {
                .NomineeID = _nomineeId, .EmployeeID = empId.Value, .Name = txtName.Text.Trim(),
                .Relation = UiKit.TextOrNothing(txtRelation), .DOB = UiKit.DateValue(dtpDob), .SharePct = nudShare.Value})
            LoadNominees()
        Catch ex As Exception
            ErrorHandler.Show(FindForm(), ex)
        End Try
    End Sub

    Private Sub OnDeleteClick(sender As Object, e As EventArgs)
        If Not _nomineeId.HasValue Then Return
        If Not ErrorHandler.Confirm(FindForm(), "Delete this nominee?") Then Return
        Try
            AppServices.Employees.DeleteNominee(_nomineeId.Value)
            LoadNominees()
        Catch ex As Exception
            ErrorHandler.Show(FindForm(), ex)
        End Try
    End Sub

    Private Sub OnClearClick(sender As Object, e As EventArgs)
        grid.ClearSelection()
        ClearInputs()
        txtName.Focus()
    End Sub
End Class
