Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Common
Imports HRMS.Models

''' <summary>Monthly attendance for every employee: days worked, holidays, leaves, overtime.
''' Ctrl+S save, F5 reload. Rows turn red when more days are entered than the employee could have worked.</summary>
Friend Class frmMonthlyAttendance
    Inherits BaseForm

    Private ReadOnly cmbMonth As New ComboBox()
    Private ReadOnly nudYear As New NumericUpDown()
    Private ReadOnly cmbSite As New ComboBox()
    Private ReadOnly btnLoad As Button = UiKit.MakeButton("Load", True)
    Private ReadOnly lblStatus As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly nudFill As New NumericUpDown()
    Private ReadOnly btnFill As Button = UiKit.MakeButton("Fill empty rows", False)
    Private ReadOnly btnCopy As Button = UiKit.MakeButton("Copy last month", False)
    Private ReadOnly btnImport As Button = UiKit.MakeButton("Import file", False)
    Private ReadOnly btnExport As Button = UiKit.MakeButton("Export CSV", False)
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save (Ctrl+S)", True)
    Private ReadOnly btnCloseMonth As Button = UiKit.MakeButton("Close month", False)
    Private ReadOnly btnReopen As Button = UiKit.MakeButton("Reopen month", False)
    Private ReadOnly lblTotals As Label = UiKit.MakeLabel(String.Empty)

    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _canClose As Boolean
    Private _year As Integer
    Private _month As Integer
    Private _table As DataTable
    Private _status As New MonthStatus()
    Private _dirty As Boolean
    Private _loading As Boolean

    Public Sub New()
        Text = "Monthly attendance"
        ClientSize = New Size(1080, 640)
        EnterMovesFocus = False
        Dim s As UserSession = AppSession.Require()
        _canEdit = s.Has("ATT_EDIT")
        _canClose = s.Has("ATT_CLOSE")

        UiKit.StyleGrid(grid)
        grid.ReadOnly = False
        grid.EditMode = DataGridViewEditMode.EditOnEnter
        grid.Dock = DockStyle.Fill
        Controls.Add(grid)

        Dim bottom As New Panel()
        bottom.Dock = DockStyle.Bottom
        bottom.Height = 104
        nudFill.DecimalPlaces = 1
        nudFill.Increment = 0.5D
        nudFill.Minimum = 0D
        nudFill.Maximum = 31D
        nudFill.Value = 26D
        nudFill.SetBounds(12, 12, 64, 26)
        btnFill.SetBounds(84, 8, 130, 32)
        btnCopy.SetBounds(222, 8, 140, 32)
        btnImport.SetBounds(370, 8, 110, 32)
        btnExport.SetBounds(488, 8, 110, 32)
        btnSave.SetBounds(12, 56, 140, 34)
        btnCloseMonth.SetBounds(160, 56, 120, 34)
        btnReopen.SetBounds(288, 56, 130, 34)
        lblTotals.Location = New Point(440, 64)
        For Each c As Control In New Control() {nudFill, btnFill, btnCopy, btnImport, btnExport, btnSave, btnCloseMonth, btnReopen, lblTotals}
            bottom.Controls.Add(c)
        Next
        Controls.Add(bottom)

        Dim top As New Panel()
        top.Dock = DockStyle.Top
        top.Height = 96
        cmbMonth.DropDownStyle = ComboBoxStyle.DropDownList
        For m As Integer = 1 To 12
            cmbMonth.Items.Add(CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m))
        Next
        nudYear.Minimum = 2000
        nudYear.Maximum = 2100
        Dim prev As Date = Date.Today.AddMonths(-1)
        cmbMonth.SelectedIndex = prev.Month - 1
        nudYear.Value = prev.Year
        UiKit.PlaceField(top, "Month", 12, 8, cmbMonth, 12, 30, 130)
        UiKit.PlaceField(top, "Year", 152, 8, nudYear, 152, 30, 80)
        UiKit.PlaceField(top, "Site", 244, 8, cmbSite, 244, 30, 220)
        btnLoad.Location = New Point(476, 26)
        top.Controls.Add(btnLoad)
        lblStatus.Location = New Point(12, 68)
        top.Controls.Add(lblStatus)
        Controls.Add(top)

        Dim header As Label = UiKit.MakeLabel("Monthly attendance")
        header.Font = Theme.TitleFont
        header.ForeColor = Theme.Primary
        header.AutoSize = False
        header.Dock = DockStyle.Top
        header.Height = 44
        header.Padding = New Padding(12, 8, 0, 0)
        Controls.Add(header)

        Try
            UiKit.BindLookup(cmbSite, AppServices.Masters.Factories(True), "FactoryID", "Name", "(all sites)")
        Catch ex As Exception
            ShowError(ex)
        End Try
        UiKit.AttachFocusColor(Me)
        AddHandler btnLoad.Click, AddressOf OnLoadClick
        AddHandler btnFill.Click, AddressOf OnFillClick
        AddHandler btnCopy.Click, AddressOf OnCopyClick
        AddHandler btnImport.Click, AddressOf OnImportClick
        AddHandler btnExport.Click, AddressOf OnExportClick
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnCloseMonth.Click, AddressOf OnCloseMonthClick
        AddHandler btnReopen.Click, AddressOf OnReopenClick
        AddHandler grid.CellFormatting, AddressOf OnCellFormatting
        AddHandler grid.DataError, AddressOf OnGridDataError
        ApplyState()
        LoadSheet()
    End Sub

    ' ---------- keyboard / closing ----------
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If e.Control AndAlso e.KeyCode = Keys.S Then
            e.Handled = True
            e.SuppressKeyPress = True
            If btnSave.Enabled Then SaveSheet()
            Return
        End If
        If e.KeyCode = Keys.F5 Then
            e.Handled = True
            LoadSheet()
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _dirty AndAlso e.CloseReason = CloseReason.UserClosing Then
            If Not Confirm("There are unsaved changes. Close without saving?") Then e.Cancel = True
        End If
        MyBase.OnFormClosing(e)
    End Sub

    ' ---------- load ----------
    Private Function ConfirmDiscard() As Boolean
        Return Not _dirty OrElse Confirm("There are unsaved changes. Discard them?")
    End Function

    Private Sub OnLoadClick(sender As Object, e As EventArgs)
        LoadSheet()
    End Sub

    Private Sub LoadSheet()
        If Not ConfirmDiscard() Then Return
        _year = CInt(nudYear.Value)
        _month = cmbMonth.SelectedIndex + 1
        _loading = True
        Try
            Dim dt As DataTable = AppServices.Attendance.Load(_year, _month, UiKit.SelectedId(cmbSite))
            Dim days As Integer = AttendanceEngine.DaysInMonth(_year, _month)
            dt.Columns.Add("Paid", GetType(Decimal), "WorkingDays + Holidays + CasualLeave + EarnedLeave + SickLeave + CompLeave")
            dt.Columns.Add("Absent", GetType(Decimal), "IIF(Paid > " & days & ", 0, " & days & " - Paid)")
            dt.AcceptChanges()
            If _table IsNot Nothing Then RemoveHandler _table.ColumnChanged, AddressOf OnColumnChanged
            _table = dt
            AddHandler _table.ColumnChanged, AddressOf OnColumnChanged
            _status = AppServices.Attendance.GetStatus(_year, _month)
            grid.DataSource = dt
            ShapeGrid()
            _dirty = False
        Catch ex As Exception
            ShowError(ex)
        Finally
            _loading = False
        End Try
        ApplyState()
        UpdateTotals()
    End Sub

    Private Sub ShapeGrid()
        UiKit.ShapeColumn(grid, "EmployeeID", Nothing, False)
        UiKit.ShapeColumn(grid, "FactoryID", Nothing, False)
        UiKit.ShapeColumn(grid, "DOJ", Nothing, False)
        UiKit.ShapeColumn(grid, "DOL", Nothing, False)
        UiKit.ShapeColumn(grid, "HasRow", Nothing, False)
        UiKit.ShapeColumn(grid, "EmpCode", "Code")
        UiKit.ShapeColumn(grid, "Name", "Name")
        UiKit.ShapeColumn(grid, "Site", "Site")
        UiKit.ShapeColumn(grid, "WorkingDays", "Worked", True, "0.0")
        UiKit.ShapeColumn(grid, "Holidays", "Holidays", True, "0.0")
        UiKit.ShapeColumn(grid, "CasualLeave", "CL", True, "0.0")
        UiKit.ShapeColumn(grid, "EarnedLeave", "EL", True, "0.0")
        UiKit.ShapeColumn(grid, "SickLeave", "SL", True, "0.0")
        UiKit.ShapeColumn(grid, "CompLeave", "Comp", True, "0.0")
        UiKit.ShapeColumn(grid, "OTHours", "OT hours", True, "0.00")
        UiKit.ShapeColumn(grid, "Paid", "Paid days", True, "0.0")
        UiKit.ShapeColumn(grid, "Absent", "Absent", True, "0.0")
        UiKit.ShapeColumn(grid, "Remarks", "Remarks")
        For Each name As String In New String() {"EmpCode", "Name", "Site", "Paid", "Absent"}
            If grid.Columns.Contains(name) Then
                grid.Columns(name).ReadOnly = True
                grid.Columns(name).DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            End If
        Next
        If grid.Columns.Contains("Name") Then grid.Columns("Name").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        If grid.Columns.Contains("Remarks") Then grid.Columns("Remarks").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    End Sub

    ''' <summary>Buttons and editing follow permission and whether the month is closed.</summary>
    Private Sub ApplyState()
        Dim closed As Boolean = _status.IsClosed
        Dim editable As Boolean = _canEdit AndAlso Not closed
        grid.ReadOnly = Not editable
        btnFill.Enabled = editable
        nudFill.Enabled = editable
        btnCopy.Enabled = editable
        btnImport.Enabled = editable
        btnSave.Enabled = editable
        btnCloseMonth.Enabled = _canClose AndAlso Not closed
        btnReopen.Enabled = _canClose AndAlso closed
        btnExport.Enabled = _table IsNot Nothing
        If _table Is Nothing Then
            lblStatus.Text = String.Empty
        ElseIf closed Then
            lblStatus.ForeColor = Theme.Danger
            lblStatus.Text = "CLOSED" & If(_status.ClosedOn.HasValue, " on " & _status.ClosedOn.Value.ToString("dd-MM-yyyy"), String.Empty) &
                If(String.IsNullOrEmpty(_status.ClosedByName), String.Empty, " by " & _status.ClosedByName) & ". Read only."
        Else
            lblStatus.ForeColor = Theme.Accent
            lblStatus.Text = "Open for entry." & If(_canEdit, String.Empty, " (View only)") &
                If(String.IsNullOrEmpty(_status.ReopenReason), String.Empty, "  Last reopened because: " & _status.ReopenReason)
        End If
    End Sub

    Private Sub OnColumnChanged(sender As Object, e As DataColumnChangeEventArgs)
        If _loading Then Return
        If e.Column.ColumnName = "Paid" OrElse e.Column.ColumnName = "Absent" Then Return
        _dirty = True
        UpdateTotals()
    End Sub

    Private Sub UpdateTotals()
        If _table Is Nothing Then
            lblTotals.Text = String.Empty
            Return
        End If
        Dim paid As Decimal = 0D
        Dim ot As Decimal = 0D
        For Each r As DataRow In _table.Rows
            paid += Convert.ToDecimal(r("Paid"))
            ot += Convert.ToDecimal(r("OTHours"))
        Next
        lblTotals.Text = _table.Rows.Count & " employees   |   paid days " & paid.ToString("0.0") & "   |   overtime " & ot.ToString("0.00") & " h" &
            If(_dirty, "   |   UNSAVED CHANGES", String.Empty)
        lblTotals.ForeColor = If(_dirty, Theme.Danger, Theme.Muted)
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse _table Is Nothing Then Return
        Dim drv As DataRowView = TryCast(grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If drv Is Nothing Then Return
        Dim r As DataRow = drv.Row
        Dim paid As Decimal = Convert.ToDecimal(r("Paid"))
        If paid <= 0D Then Return
        Dim window As Integer = AttendanceEngine.PayableWindow(_year, _month, Convert.ToDateTime(r("DOJ")),
                                    If(r.IsNull("DOL"), CType(Nothing, Date?), Convert.ToDateTime(r("DOL"))))
        If paid > window Then e.CellStyle.BackColor = Color.FromArgb(255, 220, 220)
    End Sub

    Private Sub OnGridDataError(sender As Object, e As DataGridViewDataErrorEventArgs)
        e.ThrowException = False
        e.Cancel = False
        ErrorHandler.ShowInfo(Me, "Enter a number (for example 26 or 0.5).")
    End Sub

    ' ---------- entries <-> rows ----------
    Private Shared Function ToEntry(r As DataRow) As AttendanceEntry
        Return New AttendanceEntry With {
            .EmployeeID = Convert.ToInt32(r("EmployeeID")), .EmpCode = Convert.ToString(r("EmpCode")),
            .FactoryID = If(r.IsNull("FactoryID"), CType(Nothing, Integer?), Convert.ToInt32(r("FactoryID"))),
            .WorkingDays = Convert.ToDecimal(r("WorkingDays")), .Holidays = Convert.ToDecimal(r("Holidays")),
            .CasualLeave = Convert.ToDecimal(r("CasualLeave")), .EarnedLeave = Convert.ToDecimal(r("EarnedLeave")),
            .SickLeave = Convert.ToDecimal(r("SickLeave")), .CompLeave = Convert.ToDecimal(r("CompLeave")),
            .OTHours = Convert.ToDecimal(r("OTHours")),
            .Remarks = If(r.IsNull("Remarks"), Nothing, Convert.ToString(r("Remarks")))}
    End Function

    Private Shared Sub Apply(r As DataRow, e As AttendanceEntry)
        r("WorkingDays") = e.WorkingDays
        r("Holidays") = e.Holidays
        r("CasualLeave") = e.CasualLeave
        r("EarnedLeave") = e.EarnedLeave
        r("SickLeave") = e.SickLeave
        r("CompLeave") = e.CompLeave
        r("OTHours") = e.OTHours
        If Not String.IsNullOrEmpty(e.Remarks) Then r("Remarks") = e.Remarks
    End Sub

    ' ---------- save ----------
    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        SaveSheet()
    End Sub

    Private Sub SaveSheet()
        If _table Is Nothing Then Return
        Try
            grid.EndEdit()
            Dim entries As New List(Of AttendanceEntry)()
            For Each r As DataRow In _table.Rows
                ' rows never entered and left at zero are not written
                Dim entered As Boolean = Convert.ToBoolean(r("HasRow")) OrElse Convert.ToDecimal(r("Paid")) > 0D OrElse
                    Convert.ToDecimal(r("OTHours")) > 0D OrElse r.RowState = DataRowState.Modified
                If entered Then entries.Add(ToEntry(r))
            Next
            Dim saved As Integer = AppServices.Attendance.Save(_year, _month, entries)
            _dirty = False
            ShowInfo("Saved " & saved & " employees for " & cmbMonth.Items(_month - 1).ToString() & " " & _year & ".")
            LoadSheet()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    ' ---------- helpers: fill, copy, import, export ----------
    Private Sub OnFillClick(sender As Object, e As EventArgs)
        If _table Is Nothing Then Return
        grid.EndEdit()
        Dim n As Integer = 0
        For Each r As DataRow In _table.Rows
            If Convert.ToDecimal(r("Paid")) = 0D Then
                r("WorkingDays") = nudFill.Value
                n += 1
            End If
        Next
        ShowInfo(n & " empty rows set to " & nudFill.Value.ToString("0.0") & " working days. Check them, then Save.")
    End Sub

    Private Sub OnCopyClick(sender As Object, e As EventArgs)
        If _table Is Nothing Then Return
        Try
            grid.EndEdit()
            Dim prev As New Date(_year, _month, 1)
            prev = prev.AddMonths(-1)
            Dim old As DataTable = AppServices.Attendance.Load(prev.Year, prev.Month, UiKit.SelectedId(cmbSite))
            Dim byId As New Dictionary(Of Integer, DataRow)()
            For Each r As DataRow In old.Rows
                If Convert.ToBoolean(r("HasRow")) Then byId(Convert.ToInt32(r("EmployeeID"))) = r
            Next
            Dim days As Integer = AttendanceEngine.DaysInMonth(_year, _month)
            Dim copied As Integer = 0
            Dim skipped As Integer = 0
            For Each r As DataRow In _table.Rows
                If Convert.ToDecimal(r("Paid")) <> 0D Then Continue For
                Dim src As DataRow = Nothing
                If Not byId.TryGetValue(Convert.ToInt32(r("EmployeeID")), src) Then Continue For
                Dim entry As AttendanceEntry = ToEntry(src)
                If AttendanceEngine.PaidDays(entry) > days Then
                    skipped += 1
                Else
                    Apply(r, entry)
                    copied += 1
                End If
            Next
            ShowInfo("Copied " & copied & " rows from " & prev.ToString("MMMM yyyy", CultureInfo.InvariantCulture) & "." &
                     If(skipped > 0, Environment.NewLine & skipped & " rows skipped (more days than this month has).", String.Empty) &
                     Environment.NewLine & "Check them, then Save.")
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnImportClick(sender As Object, e As EventArgs)
        If _table Is Nothing Then Return
        Try
            Using dlg As New OpenFileDialog()
                dlg.Title = "Choose the attendance file"
                dlg.Filter = "Excel or CSV (*.xlsx;*.csv)|*.xlsx;*.csv|All files (*.*)|*.*"
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                grid.EndEdit()
                Dim codeToId As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                Dim rowById As New Dictionary(Of Integer, DataRow)()
                For Each r As DataRow In _table.Rows
                    codeToId(Convert.ToString(r("EmpCode"))) = Convert.ToInt32(r("EmployeeID"))
                    rowById(Convert.ToInt32(r("EmployeeID"))) = r
                Next
                Dim result As ImportResult = AppServices.Attendance.ParseImport(TabularFile.Read(dlg.FileName), codeToId, _year, _month)
                For Each entry As AttendanceEntry In result.Entries
                    Apply(rowById(entry.EmployeeID), entry)
                Next
                Dim msg As String = result.Entries.Count & " of " & result.RowsRead & " rows applied. Check them, then Save."
                If result.Errors.Count > 0 Then
                    Dim shown As Integer = Math.Min(result.Errors.Count, 12)
                    msg &= Environment.NewLine & Environment.NewLine & result.Errors.Count & " rows had problems and were skipped:" & Environment.NewLine &
                        String.Join(Environment.NewLine, result.Errors.GetRange(0, shown)) &
                        If(result.Errors.Count > shown, Environment.NewLine & "...", String.Empty)
                End If
                ShowInfo(msg)
            End Using
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnExportClick(sender As Object, e As EventArgs)
        If _table Is Nothing Then Return
        Try
            Using dlg As New SaveFileDialog()
                dlg.Title = "Save attendance as CSV (also usable as an import template)"
                dlg.Filter = "CSV file (*.csv)|*.csv"
                dlg.FileName = "attendance_" & _year & "_" & _month.ToString("00", CultureInfo.InvariantCulture) & ".csv"
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                grid.EndEdit()
                Dim rows As New List(Of String())()
                rows.Add(New String() {"Code", "Name", "WD", "HD", "CL", "EL", "SL", "Comp", "OT", "Remarks"})
                For Each r As DataRow In _table.Rows
                    Dim en As AttendanceEntry = ToEntry(r)
                    rows.Add(New String() {en.EmpCode, Convert.ToString(r("Name")), Num(en.WorkingDays), Num(en.Holidays), Num(en.CasualLeave),
                                           Num(en.EarnedLeave), Num(en.SickLeave), Num(en.CompLeave), Num(en.OTHours), If(en.Remarks, String.Empty)})
                Next
                TabularFile.WriteCsv(dlg.FileName, rows)
                ShowInfo("Saved " & (rows.Count - 1) & " rows.")
            End Using
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Shared Function Num(v As Decimal) As String
        Return v.ToString("0.##", CultureInfo.InvariantCulture)
    End Function

    ' ---------- close / reopen ----------
    Private Sub OnCloseMonthClick(sender As Object, e As EventArgs)
        If _table Is Nothing Then Return
        If _dirty Then
            ShowInfo("Save your changes before closing the month.")
            Return
        End If
        If Not Confirm("Close " & cmbMonth.Items(_month - 1).ToString() & " " & _year & "? Attendance will be locked until it is reopened with a reason.") Then Return
        Try
            AppServices.Attendance.CloseMonth(_year, _month)
            LoadSheet()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnReopenClick(sender As Object, e As EventArgs)
        If _table Is Nothing Then Return
        Try
            Using dlg As New frmTextPrompt("Reopen month", "Reason (written to the log)", 10, 200)
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                AppServices.Attendance.ReopenMonth(_year, _month, dlg.Value)
            End Using
            LoadSheet()
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
