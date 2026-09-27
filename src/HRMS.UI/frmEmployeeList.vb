Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>Employee search grid. F2 new, F3 search box, F5 refresh, Enter/double-click opens.</summary>
Friend Class frmEmployeeList
    Inherits BaseForm

    Private Const PAGE_SIZE As Integer = 100

    Private ReadOnly txtSearch As TextBox = UiKit.MakeText()
    Private ReadOnly cmbFactory As New ComboBox()
    Private ReadOnly cmbDept As New ComboBox()
    Private ReadOnly chkActive As New CheckBox()
    Private ReadOnly btnSearch As Button = UiKit.MakeButton("Search", True)
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly btnNew As Button = UiKit.MakeButton("New (F2)", True)
    Private ReadOnly btnOpen As Button = UiKit.MakeButton("Open", False)
    Private ReadOnly btnExit As Button = UiKit.MakeButton("Exit employee", False)
    Private ReadOnly btnReactivate As Button = UiKit.MakeButton("Reactivate", False)
    Private ReadOnly btnPrev As Button = UiKit.MakeButton("< Prev", False)
    Private ReadOnly btnNext As Button = UiKit.MakeButton("Next >", False)
    Private ReadOnly lblPage As Label = UiKit.MakeLabel(String.Empty)
    Private _page As Integer = 1
    Private _pages As Integer = 1
    Private _loading As Boolean
    Private ReadOnly _canEdit As Boolean

    Public Sub New()
        Text = "Employees"
        ClientSize = New Size(1020, 620)
        EnterMovesFocus = False
        _canEdit = AppSession.Require().Has("EMPLOYEE_EDIT")

        UiKit.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Controls.Add(grid)

        Dim bottom As New Panel()
        bottom.Dock = DockStyle.Bottom
        bottom.Height = 54
        btnNew.Location = New Point(12, 11)
        btnOpen.Location = New Point(130, 11)
        btnExit.Location = New Point(248, 11)
        btnExit.Width = 130
        btnReactivate.Location = New Point(388, 11)
        btnPrev.Location = New Point(720, 11)
        btnPrev.Width = 90
        btnNext.Location = New Point(900, 11)
        btnNext.Width = 90
        lblPage.AutoSize = False
        lblPage.TextAlign = ContentAlignment.MiddleCenter
        lblPage.SetBounds(812, 11, 86, 32)
        For Each c As Control In New Control() {btnNew, btnOpen, btnExit, btnReactivate, btnPrev, btnNext, lblPage}
            bottom.Controls.Add(c)
        Next
        Controls.Add(bottom)

        Dim filters As New Panel()
        filters.Dock = DockStyle.Top
        filters.Height = 70
        UiKit.PlaceField(filters, "Search (code, name, mobile)", 12, 8, txtSearch, 12, 30, 230)
        UiKit.PlaceField(filters, "Factory", 258, 8, cmbFactory, 258, 30, 170)
        UiKit.PlaceField(filters, "Department", 442, 8, cmbDept, 442, 30, 170)
        chkActive.Text = "Active only"
        chkActive.Checked = True
        chkActive.AutoSize = True
        chkActive.Location = New Point(628, 32)
        filters.Controls.Add(chkActive)
        btnSearch.Location = New Point(760, 26)
        filters.Controls.Add(btnSearch)
        Controls.Add(filters)

        Dim header As Label = UiKit.MakeLabel("Employees")
        header.Font = Theme.TitleFont
        header.ForeColor = Theme.Primary
        header.AutoSize = False
        header.Dock = DockStyle.Top
        header.Height = 44
        header.Padding = New Padding(12, 8, 0, 0)
        Controls.Add(header)

        btnNew.Enabled = _canEdit
        UiKit.AttachFocusColor(Me)
        AddHandler btnSearch.Click, AddressOf OnSearchClick
        AddHandler txtSearch.KeyDown, AddressOf OnSearchKeyDown
        AddHandler btnNew.Click, AddressOf OnNewClick
        AddHandler btnOpen.Click, AddressOf OnOpenClick
        AddHandler grid.DoubleClick, AddressOf OnOpenClick
        AddHandler grid.KeyDown, AddressOf OnGridKeyDown
        AddHandler grid.SelectionChanged, AddressOf OnRowChanged
        AddHandler btnExit.Click, AddressOf OnExitClick
        AddHandler btnReactivate.Click, AddressOf OnReactivateClick
        AddHandler btnPrev.Click, AddressOf OnPrevClick
        AddHandler btnNext.Click, AddressOf OnNextClick

        LoadFilters()
        LoadPage(Nothing)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        Select Case e.KeyCode
            Case Keys.F2
                If _canEdit Then OpenEditor(Nothing)
                e.Handled = True
            Case Keys.F3
                txtSearch.Focus()
                txtSearch.SelectAll()
                e.Handled = True
            Case Keys.F5
                LoadPage(SelectedId())
                e.Handled = True
            Case Keys.Escape
                Close()
                e.Handled = True
        End Select
    End Sub

    Private Sub LoadFilters()
        Try
            UiKit.BindLookup(cmbFactory, AppServices.Masters.Factories(True), "FactoryID", "Name", "(All factories)")
            UiKit.BindLookup(cmbDept, AppServices.Masters.Departments(True), "DeptID", "Name", "(All departments)")
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Function SelectedId() As Integer?
        Return UiKit.SelectedRowId(grid, "EmployeeID")
    End Function

    Private Sub LoadPage(selectId As Integer?)
        _loading = True
        Try
            Dim dt As DataTable = AppServices.Employees.Search(UiKit.TextOrNothing(txtSearch), UiKit.SelectedId(cmbFactory),
                UiKit.SelectedId(cmbDept), chkActive.Checked, _page, PAGE_SIZE)
            Dim total As Integer = If(dt.Rows.Count > 0, Convert.ToInt32(dt.Rows(0)("TotalRows")), 0)
            _pages = Math.Max(1, CInt(Math.Ceiling(total / CDbl(PAGE_SIZE))))
            grid.DataSource = dt
            UiKit.ShapeColumn(grid, "EmployeeID", Nothing, False)
            UiKit.ShapeColumn(grid, "TotalRows", Nothing, False)
            UiKit.ShapeColumn(grid, "EmpCode", "Code")
            UiKit.ShapeColumn(grid, "Name", "Name")
            UiKit.ShapeColumn(grid, "FatherName", "Father / Husband")
            UiKit.ShapeColumn(grid, "DOJ", "Joined", True, "dd-MM-yyyy")
            UiKit.ShapeColumn(grid, "DOL", "Left", True, "dd-MM-yyyy")
            UiKit.ShapeColumn(grid, "Mobile", "Mobile")
            UiKit.ShapeColumn(grid, "Factory", "Factory")
            UiKit.ShapeColumn(grid, "Department", "Department")
            UiKit.ShapeColumn(grid, "Designation", "Designation")
            UiKit.ShapeColumn(grid, "IsActive", "Active")
            lblPage.Text = "Page " & _page & " / " & _pages & Environment.NewLine & total & " rows"
            lblPage.Font = Theme.SmallFont
            btnPrev.Enabled = _page > 1
            btnNext.Enabled = _page < _pages
            If selectId.HasValue Then
                For Each row As DataGridViewRow In grid.Rows
                    If Convert.ToInt32(row.Cells("EmployeeID").Value) = selectId.Value Then
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
        UpdateButtons()
    End Sub

    Private Sub UpdateButtons()
        Dim hasRow As Boolean = grid.CurrentRow IsNot Nothing
        btnOpen.Enabled = hasRow
        Dim active As Boolean = hasRow AndAlso Convert.ToBoolean(grid.CurrentRow.Cells("IsActive").Value)
        btnExit.Enabled = _canEdit AndAlso hasRow AndAlso active
        btnReactivate.Enabled = _canEdit AndAlso hasRow AndAlso Not active
    End Sub

    Private Sub OnRowChanged(sender As Object, e As EventArgs)
        If Not _loading Then UpdateButtons()
    End Sub

    Private Sub OnSearchClick(sender As Object, e As EventArgs)
        _page = 1
        LoadPage(Nothing)
    End Sub

    Private Sub OnSearchKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            OnSearchClick(sender, e)
        End If
    End Sub

    Private Sub OnPrevClick(sender As Object, e As EventArgs)
        If _page > 1 Then
            _page -= 1
            LoadPage(Nothing)
        End If
    End Sub

    Private Sub OnNextClick(sender As Object, e As EventArgs)
        If _page < _pages Then
            _page += 1
            LoadPage(Nothing)
        End If
    End Sub

    Private Sub OnNewClick(sender As Object, e As EventArgs)
        OpenEditor(Nothing)
    End Sub

    Private Sub OnOpenClick(sender As Object, e As EventArgs)
        Dim id As Integer? = SelectedId()
        If id.HasValue Then OpenEditor(id)
    End Sub

    Private Sub OnGridKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            OnOpenClick(sender, e)
        End If
    End Sub

    Private Sub OpenEditor(employeeID As Integer?)
        Dim savedId As Integer? = employeeID
        Try
            Using dlg As New frmEmployeeEdit(employeeID)
                dlg.ShowDialog(Me)
                If dlg.EmployeeID.HasValue Then savedId = dlg.EmployeeID
            End Using
        Catch ex As Exception
            ShowError(ex)
        End Try
        LoadFilters()
        LoadPage(savedId)
    End Sub

    Private Sub OnExitClick(sender As Object, e As EventArgs)
        Dim id As Integer? = SelectedId()
        If Not id.HasValue Then Return
        Try
            Using dlg As New frmPickDate("Leaving date", "Date the employee left", Date.Today)
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                AppServices.Employees.ExitEmployee(id.Value, dlg.Value)
            End Using
            LoadPage(id)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnReactivateClick(sender As Object, e As EventArgs)
        Dim id As Integer? = SelectedId()
        If Not id.HasValue Then Return
        If Not Confirm("Mark this employee as working again?") Then Return
        Try
            AppServices.Employees.Reactivate(id.Value)
            LoadPage(id)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub
End Class
