Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Models

''' <summary>Daily attendance entry for one date. F5 Load, Ctrl+S Save, Esc Close. Only meaningful for a
''' company running in Daily mode (AttendanceService.GetMode/SetMode) - Monthly-mode companies keep
''' using the existing monthly attendance screen. 'L' is not offered as a status here; apply leave
''' through frmLeave instead, and it will show up automatically wherever totals are computed.</summary>
Public Class frmDailyAttendance
    Inherits Form

    Private ReadOnly svc As New AttendanceService()
    Private ReadOnly masterSvc As New MasterService()
    Private dtpDate As DateTimePicker
    Private cboFactory As ComboBox
    Private dgv As DataGridView
    Private btnLoad As Button
    Private btnSave As Button
    Private lblStatus As Label

    Public Sub New()
        BuildLayout()
        AddHandler Me.Load, AddressOf frmDailyAttendance_Load
        AddHandler Me.KeyDown, AddressOf frmDailyAttendance_KeyDown
        Me.KeyPreview = True
    End Sub

    Private Sub BuildLayout()
        Me.Text = "Daily Attendance"
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.Size = New Size(980, 560)
        Me.StartPosition = FormStartPosition.CenterParent

        Dim top As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 40, .Padding = New Padding(8)}
        dtpDate = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 110}
        cboFactory = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 180, .DisplayMember = "Text"}
        btnLoad = New Button() With {.Text = "Load (F5)", .Width = 90}
        btnSave = New Button() With {.Text = "Save (Ctrl+S)", .Width = 110}
        AddHandler btnLoad.Click, AddressOf btnLoad_Click
        AddHandler btnSave.Click, AddressOf btnSave_Click
        top.Controls.AddRange(New Control() {
            New Label() With {.Text = "Date", .AutoSize = True, .Padding = New Padding(0, 6, 4, 0)}, dtpDate,
            New Label() With {.Text = "Factory", .AutoSize = True, .Padding = New Padding(12, 6, 4, 0)}, cboFactory,
            btnLoad, btnSave})

        dgv = New DataGridView() With {
            .Dock = DockStyle.Fill, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .AutoGenerateColumns = False, .SelectionMode = DataGridViewSelectionMode.CellSelect
        }

        lblStatus = New Label() With {.Dock = DockStyle.Bottom, .Height = 24, .ForeColor = Color.DimGray, .Padding = New Padding(8, 4, 0, 0)}

        Me.Controls.Add(dgv)
        Me.Controls.Add(lblStatus)
        Me.Controls.Add(top)
    End Sub

    Private Sub frmDailyAttendance_Load(sender As Object, e As EventArgs)
        dtpDate.Value = Date.Today
        LoadFactories()
        BuildGridColumns()
        LoadDay()
    End Sub

    Private Sub frmDailyAttendance_KeyDown(sender As Object, e As KeyEventArgs)
        Select Case True
            Case e.KeyCode = Keys.F5 : LoadDay()
            Case e.Control AndAlso e.KeyCode = Keys.S : btnSave_Click(sender, e) : e.SuppressKeyPress = True
            Case e.KeyCode = Keys.Escape : Me.Close()
        End Select
    End Sub

    Private Sub LoadFactories()
        cboFactory.Items.Clear()
        cboFactory.Items.Add(New ComboItem(Nothing, "(All)"))
        Dim dt As DataTable = masterSvc.Factories(True)
        For Each r As DataRow In dt.Rows
            cboFactory.Items.Add(New ComboItem(Convert.ToInt32(r("FactoryID")), Convert.ToString(r("Name"))))
        Next
        cboFactory.SelectedIndex = 0
    End Sub

    Private Sub BuildGridColumns()
        dgv.Columns.Clear()
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "EmployeeID", .Visible = False})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "EmpCode", .HeaderText = "Code", .ReadOnly = True, .Width = 70})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Name", .HeaderText = "Employee", .ReadOnly = True, .Width = 170})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Site", .HeaderText = "Site", .ReadOnly = True, .Width = 110})

        Dim statusCol As New DataGridViewComboBoxColumn() With {.Name = "Status", .HeaderText = "Status", .Width = 70}
        statusCol.Items.AddRange(AttendanceEngine.DailyStatuses)
        dgv.Columns.Add(statusCol)

        Dim shiftCol As New DataGridViewComboBoxColumn() With {
            .Name = "ShiftID", .HeaderText = "Shift", .Width = 150, .ValueMember = "ShiftID", .DisplayMember = "Name"
        }
        ' All shifts (not just active) so a previously-assigned-then-deactivated shift still displays.
        shiftCol.DataSource = masterSvc.Shifts(False)
        dgv.Columns.Add(shiftCol)

        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "InTime", .HeaderText = "In (HH:mm)", .Width = 80})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "OutTime", .HeaderText = "Out (HH:mm)", .Width = 80})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "OTHours", .HeaderText = "OT hrs", .Width = 60})
    End Sub

    Private Sub btnLoad_Click(sender As Object, e As EventArgs)
        LoadDay()
    End Sub

    Private Sub LoadDay()
        Try
            Dim factoryID As Integer? = CType(cboFactory.SelectedItem, ComboItem)?.Value
            Dim dt As DataTable = svc.LoadDay(dtpDate.Value.Date, factoryID)
            dgv.Rows.Clear()
            For Each r As DataRow In dt.Rows
                Dim idx As Integer = dgv.Rows.Add()
                Dim row As DataGridViewRow = dgv.Rows(idx)
                row.Cells("EmployeeID").Value = r("EmployeeID")
                row.Cells("EmpCode").Value = r("EmpCode")
                row.Cells("Name").Value = r("Name")
                row.Cells("Site").Value = If(IsDBNull(r("Site")), String.Empty, r("Site"))
                row.Cells("Status").Value = If(IsDBNull(r("Status")), "P", Convert.ToString(r("Status")))
                row.Cells("ShiftID").Value = If(IsDBNull(r("ShiftID")), CType(Nothing, Object), r("ShiftID"))
                row.Cells("InTime").Value = If(IsDBNull(r("InTime")), String.Empty, CType(r("InTime"), TimeSpan).ToString("hh\:mm"))
                row.Cells("OutTime").Value = If(IsDBNull(r("OutTime")), String.Empty, CType(r("OutTime"), TimeSpan).ToString("hh\:mm"))
                row.Cells("OTHours").Value = Convert.ToDecimal(r("OTHours")).ToString("0.00")
            Next
            lblStatus.Text = dt.Rows.Count & " employees for " & dtpDate.Value.ToString("dd-MMM-yyyy")
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        Try
            Dim entries As New List(Of DailyAttendanceEntry)()
            For Each row As DataGridViewRow In dgv.Rows
                If row.IsNewRow Then Continue For
                entries.Add(New DailyAttendanceEntry() With {
                    .EmployeeID = Convert.ToInt32(row.Cells("EmployeeID").Value),
                    .EmpCode = Convert.ToString(row.Cells("EmpCode").Value),
                    .Status = Convert.ToString(row.Cells("Status").Value),
                    .ShiftID = ParseNullableInt(row.Cells("ShiftID").Value),
                    .InTime = ParseTime(row.Cells("InTime").Value),
                    .OutTime = ParseTime(row.Cells("OutTime").Value),
                    .OTHours = ParseDecimal(row.Cells("OTHours").Value)
                })
            Next
            Dim saved As Integer = svc.SaveDay(dtpDate.Value.Date, entries)
            lblStatus.Text = saved & " rows saved."
            LoadDay()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Shared Function ParseNullableInt(v As Object) As Integer?
        If v Is Nothing OrElse v Is DBNull.Value OrElse String.IsNullOrEmpty(Convert.ToString(v)) Then Return Nothing
        Return Convert.ToInt32(v)
    End Function

    Private Shared Function ParseDecimal(v As Object) As Decimal
        Dim d As Decimal
        If v IsNot Nothing AndAlso Decimal.TryParse(Convert.ToString(v), NumberStyles.Float, CultureInfo.InvariantCulture, d) Then Return d
        Return 0D
    End Function

    Private Shared Function ParseTime(v As Object) As TimeSpan?
        Dim text As String = Convert.ToString(v)
        If String.IsNullOrWhiteSpace(text) Then Return Nothing
        Dim ts As TimeSpan
        If TimeSpan.TryParseExact(text.Trim(), "hh\:mm", CultureInfo.InvariantCulture, ts) Then Return ts
        If TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, ts) Then Return ts
        Return Nothing
    End Function

    Private Sub ShowError(ex As Exception)
        lblStatus.Text = ex.Message
        MessageBox.Show(Me, ex.Message, "Daily Attendance", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private NotInheritable Class ComboItem
        Public ReadOnly Property Value As Integer?
        Public ReadOnly Property Text As String
        Public Sub New(value As Integer?, text As String)
            Me.Value = value
            Me.Text = text
        End Sub
        Public Overrides Function ToString() As String
            Return Text
        End Function
    End Class
End Class
