Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Models

''' <summary>Leave: apply, list/filter, approve/reject/cancel, CL/SL/EL balance. Employee picker uses
''' MasterService.EmployeeLookup() (a thin read-only wrapper) - swap in the project's real employee
''' picker if one already exists. Reject uses Microsoft.VisualBasic.Interaction.InputBox for a one-line
''' reason prompt; replace with UiKit's own prompt dialog once that's available.</summary>
Public Class frmLeave
    Inherits Form

    Private ReadOnly svc As New LeaveService()
    Private ReadOnly masterSvc As New MasterService()

    Private cboEmployee As ComboBox
    Private cboType As ComboBox
    Private dtpFrom As DateTimePicker
    Private dtpTo As DateTimePicker
    Private txtReason As TextBox
    Private lblDays As Label
    Private lblBalance As Label
    Private btnApply As Button

    Private cboFilterStatus As ComboBox
    Private btnRefresh As Button
    Private dgvList As DataGridView
    Private btnApprove As Button
    Private btnReject As Button
    Private btnCancel As Button
    Private lblStatus As Label

    Public Sub New()
        BuildLayout()
        AddHandler Me.Load, AddressOf frmLeave_Load
        AddHandler Me.KeyDown, AddressOf frmLeave_KeyDown
        Me.KeyPreview = True
    End Sub

    Private Sub BuildLayout()
        Me.Text = "Leave"
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.Size = New Size(980, 640)
        Me.StartPosition = FormStartPosition.CenterParent

        Dim applyPanel As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 40, .Padding = New Padding(8)}
        cboEmployee = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220, .DisplayMember = "Text"}
        cboType = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 70}
        cboType.Items.AddRange(New String() {"CL", "SL", "EL", "ML", "LWP"})
        dtpFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 100}
        dtpTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 100}
        txtReason = New TextBox() With {.Width = 220}
        btnApply = New Button() With {.Text = "Apply", .Width = 80}
        lblDays = New Label() With {.AutoSize = True, .Padding = New Padding(4, 6, 0, 0)}
        AddHandler cboEmployee.SelectedIndexChanged, AddressOf RefreshBalance
        AddHandler dtpFrom.ValueChanged, AddressOf DateChanged
        AddHandler dtpTo.ValueChanged, AddressOf DateChanged
        AddHandler btnApply.Click, AddressOf btnApply_Click
        applyPanel.Controls.AddRange(New Control() {
            New Label() With {.Text = "Employee", .AutoSize = True, .Padding = New Padding(0, 6, 4, 0)}, cboEmployee,
            New Label() With {.Text = "Type", .AutoSize = True, .Padding = New Padding(8, 6, 4, 0)}, cboType,
            New Label() With {.Text = "From", .AutoSize = True, .Padding = New Padding(8, 6, 4, 0)}, dtpFrom,
            New Label() With {.Text = "To", .AutoSize = True, .Padding = New Padding(8, 6, 4, 0)}, dtpTo,
            New Label() With {.Text = "Reason", .AutoSize = True, .Padding = New Padding(8, 6, 4, 0)}, txtReason,
            btnApply, lblDays})

        lblBalance = New Label() With {.Dock = DockStyle.Top, .Height = 22, .ForeColor = Color.DimGray, .Padding = New Padding(8, 0, 0, 0)}

        Dim filterRow As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 32, .Padding = New Padding(8)}
        cboFilterStatus = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 110}
        cboFilterStatus.Items.AddRange(New String() {"All", "Pending", "Approved", "Rejected"})
        cboFilterStatus.SelectedIndex = 0
        btnRefresh = New Button() With {.Text = "Refresh (F5)", .Width = 100}
        AddHandler btnRefresh.Click, AddressOf btnRefresh_Click
        filterRow.Controls.AddRange(New Control() {
            New Label() With {.Text = "Status", .AutoSize = True, .Padding = New Padding(0, 6, 4, 0)}, cboFilterStatus, btnRefresh})

        dgvList = New DataGridView() With {
            .Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }

        Dim actionRow As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 40, .Padding = New Padding(8)}
        btnApprove = New Button() With {.Text = "Approve", .Width = 90}
        btnReject = New Button() With {.Text = "Reject", .Width = 90}
        btnCancel = New Button() With {.Text = "Cancel", .Width = 90}
        AddHandler btnApprove.Click, AddressOf btnApprove_Click
        AddHandler btnReject.Click, AddressOf btnReject_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        actionRow.Controls.AddRange(New Control() {btnApprove, btnReject, btnCancel})

        lblStatus = New Label() With {.Dock = DockStyle.Bottom, .Height = 24, .ForeColor = Color.DimGray, .Padding = New Padding(8, 4, 0, 0)}

        Me.Controls.Add(dgvList)
        Me.Controls.Add(actionRow)
        Me.Controls.Add(lblStatus)
        Me.Controls.Add(filterRow)
        Me.Controls.Add(lblBalance)
        Me.Controls.Add(applyPanel)
    End Sub

    Private Sub frmLeave_Load(sender As Object, e As EventArgs)
        dtpFrom.Value = Date.Today
        dtpTo.Value = Date.Today
        LoadEmployees()
        cboType.SelectedIndex = 0
        RefreshList()
        RefreshBalance(sender, e)
        DateChanged(sender, e)
    End Sub

    Private Sub frmLeave_KeyDown(sender As Object, e As KeyEventArgs)
        Select Case True
            Case e.KeyCode = Keys.F5 : RefreshList()
            Case e.KeyCode = Keys.Escape : Me.Close()
        End Select
    End Sub

    Private Sub LoadEmployees()
        cboEmployee.Items.Clear()
        Dim dt As DataTable = masterSvc.EmployeeLookup()
        For Each r As DataRow In dt.Rows
            cboEmployee.Items.Add(New ComboItem(Convert.ToInt32(r("EmployeeID")),
                Convert.ToString(r("EmpCode")) & " - " & Convert.ToString(r("Name"))))
        Next
        If cboEmployee.Items.Count > 0 Then cboEmployee.SelectedIndex = 0
    End Sub

    Private Sub DateChanged(sender As Object, e As EventArgs)
        Dim days As Integer = CInt((dtpTo.Value.Date - dtpFrom.Value.Date).TotalDays) + 1
        lblDays.Text = If(days > 0, days & " day(s)", String.Empty)
    End Sub

    Private Sub RefreshBalance(sender As Object, e As EventArgs)
        Dim item As ComboItem = TryCast(cboEmployee.SelectedItem, ComboItem)
        If item Is Nothing Then Return
        Try
            Dim b As LeaveBalance = svc.GetBalance(item.Value, Date.Today)
            lblBalance.Text = String.Format("CL {0:0.#}/{1:0.#}    SL {2:0.#}/{3:0.#}    EL {4:0.#}/{5:0.#}  (as of {6:dd-MMM-yyyy})",
                b.CLBalance, b.CLEntitlement, b.SLBalance, b.SLEntitlement, b.ELBalance, b.ELEarned, b.AsOfDate)
        Catch ex As Exception
            lblBalance.Text = String.Empty
        End Try
    End Sub

    Private Sub btnApply_Click(sender As Object, e As EventArgs)
        Dim item As ComboItem = TryCast(cboEmployee.SelectedItem, ComboItem)
        If item Is Nothing Then Return
        Try
            svc.Apply(item.Value, Convert.ToString(cboType.SelectedItem), dtpFrom.Value.Date, dtpTo.Value.Date, txtReason.Text)
            txtReason.Text = String.Empty
            lblStatus.Text = "Leave application submitted."
            RefreshList()
            RefreshBalance(sender, e)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs)
        RefreshList()
    End Sub

    Private Sub RefreshList()
        Try
            Dim status As String = If(cboFilterStatus.SelectedIndex <= 0, Nothing, Convert.ToString(cboFilterStatus.SelectedItem))
            dgvList.DataSource = svc.List(Nothing, status, Nothing)
            If dgvList.Columns.Contains("EmployeeID") Then dgvList.Columns("EmployeeID").Visible = False
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Function SelectedLeaveID() As Integer?
        If dgvList.CurrentRow Is Nothing Then Return Nothing
        Dim row As DataRowView = TryCast(dgvList.CurrentRow.DataBoundItem, DataRowView)
        If row Is Nothing Then Return Nothing
        Return Convert.ToInt32(row("LeaveID"))
    End Function

    Private Sub btnApprove_Click(sender As Object, e As EventArgs)
        Dim id As Integer? = SelectedLeaveID()
        If Not id.HasValue Then Return
        Try
            svc.Approve(id.Value, Nothing)
            lblStatus.Text = "Leave " & id.Value & " approved."
            RefreshList()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub btnReject_Click(sender As Object, e As EventArgs)
        Dim id As Integer? = SelectedLeaveID()
        If Not id.HasValue Then Return
        Dim reason As String = Microsoft.VisualBasic.Interaction.InputBox("Reason for rejecting:", "Reject Leave", String.Empty)
        If String.IsNullOrWhiteSpace(reason) Then Return
        Try
            svc.Reject(id.Value, reason)
            lblStatus.Text = "Leave " & id.Value & " rejected."
            RefreshList()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Dim id As Integer? = SelectedLeaveID()
        If Not id.HasValue Then Return
        If MessageBox.Show(Me, "Cancel this leave application?", "Leave", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        Try
            svc.Cancel(id.Value)
            lblStatus.Text = "Leave " & id.Value & " cancelled."
            RefreshList()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub ShowError(ex As Exception)
        lblStatus.Text = ex.Message
        MessageBox.Show(Me, ex.Message, "Leave", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private NotInheritable Class ComboItem
        Public ReadOnly Property Value As Integer
        Public ReadOnly Property Text As String
        Public Sub New(value As Integer, text As String)
            Me.Value = value
            Me.Text = text
        End Sub
        Public Overrides Function ToString() As String
            Return Text
        End Function
    End Class
End Class
