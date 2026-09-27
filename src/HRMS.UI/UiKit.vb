Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Small helpers so every form looks and behaves the same.</summary>
Friend NotInheritable Class UiKit
    Private Sub New()
    End Sub

    Public Shared Function MakeLabel(text As String) As Label
        Dim l As New Label()
        l.Text = text
        l.AutoSize = True
        l.BackColor = Color.Transparent
        Return l
    End Function

    Public Shared Function MakeText(Optional isPassword As Boolean = False) As TextBox
        Dim t As New TextBox()
        t.BorderStyle = BorderStyle.FixedSingle
        t.UseSystemPasswordChar = isPassword
        Return t
    End Function

    Public Shared Function MakeButton(text As String, isPrimary As Boolean) As Button
        Dim b As New Button()
        b.Text = text
        b.Size = New Size(110, 32)
        b.FlatStyle = FlatStyle.Flat
        b.UseVisualStyleBackColor = False
        b.Cursor = Cursors.Hand
        If isPrimary Then
            b.BackColor = Theme.Primary
            b.ForeColor = Color.White
            b.FlatAppearance.BorderSize = 0
        Else
            b.BackColor = Color.White
            b.ForeColor = Theme.Primary
            b.FlatAppearance.BorderColor = Theme.Primary
        End If
        Return b
    End Function

    ''' <summary>Title strip across the top of a dialog.</summary>
    Public Shared Function MakeHeader(title As String, subtitle As String, width As Integer, height As Integer) As Panel
        Dim p As New Panel()
        p.BackColor = Theme.Primary
        p.SetBounds(0, 0, width, height)
        Dim t As Label = MakeLabel(title)
        t.Font = Theme.TitleFont
        t.ForeColor = Color.White
        t.Location = New Point(24, 10)
        p.Controls.Add(t)
        If Not String.IsNullOrEmpty(subtitle) Then
            Dim s As Label = MakeLabel(subtitle)
            s.Font = Theme.SubtitleFont
            s.ForeColor = Color.White
            s.Location = New Point(26, height - 28)
            p.Controls.Add(s)
        End If
        Return p
    End Function

    ''' <summary>Adds a caption above/left of a control and positions the control.</summary>
    Public Shared Sub PlaceField(parent As Control, caption As String, capX As Integer, capY As Integer,
                                 ctl As Control, x As Integer, y As Integer, w As Integer)
        Dim l As Label = MakeLabel(caption)
        l.Location = New Point(capX, capY)
        parent.Controls.Add(l)
        ctl.Location = New Point(x, y)
        ctl.Width = w
        parent.Controls.Add(ctl)
    End Sub

    ''' <summary>Highlights the focused text box / combo box (colour from design.md).</summary>
    Public Shared Sub AttachFocusColor(root As Control)
        For Each c As Control In root.Controls
            If TypeOf c Is TextBox OrElse TypeOf c Is ComboBox Then
                AddHandler c.Enter, Sub(s As Object, e As EventArgs) DirectCast(s, Control).BackColor = Theme.InputFocus
                AddHandler c.Leave, Sub(s As Object, e As EventArgs) DirectCast(s, Control).BackColor = Color.White
            End If
            If c.HasChildren Then AttachFocusColor(c)
        Next
    End Sub
    ' ---------- grids ----------
    Public Shared Sub StyleGrid(g As DataGridView)
        g.ReadOnly = True
        g.AllowUserToAddRows = False
        g.AllowUserToDeleteRows = False
        g.AllowUserToResizeRows = False
        g.MultiSelect = False
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        g.RowHeadersVisible = False
        g.BackgroundColor = Color.White
        g.BorderStyle = BorderStyle.FixedSingle
        g.EnableHeadersVisualStyles = False
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        g.ColumnHeadersHeight = 30
        g.ColumnHeadersDefaultCellStyle.BackColor = Theme.Primary
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Primary
        g.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAlt
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(198, 218, 240)
        g.DefaultCellStyle.SelectionForeColor = Color.Black
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
    End Sub

    ''' <summary>Hides a column and/or renames its header (no error if the column is absent).</summary>
    Public Shared Sub ShapeColumn(g As DataGridView, name As String, header As String, Optional visible As Boolean = True, Optional format As String = Nothing)
        If Not g.Columns.Contains(name) Then Return
        Dim col As DataGridViewColumn = g.Columns(name)
        col.Visible = visible
        If header IsNot Nothing Then col.HeaderText = header
        If format IsNot Nothing Then col.DefaultCellStyle.Format = format
    End Sub

    ''' <summary>ID of the selected row (first column named idColumn), or Nothing.</summary>
    Public Shared Function SelectedRowId(g As DataGridView, idColumn As String) As Integer?
        If g.CurrentRow Is Nothing Then Return Nothing
        Dim v As Object = g.CurrentRow.Cells(idColumn).Value
        If v Is Nothing OrElse v Is DBNull.Value Then Return Nothing
        Return Convert.ToInt32(v)
    End Function

    ' ---------- combo boxes ----------
    ''' <summary>Fills a combo from a lookup table with a first "no selection" row (ID = null).</summary>
    Public Shared Sub BindLookup(cmb As ComboBox, source As DataTable, idColumn As String, nameColumn As String, firstCaption As String)
        Dim dt As DataTable = source.Clone()
        If Not dt.Columns.Contains(idColumn) Then dt.Columns.Add(idColumn, GetType(Integer))
        If Not dt.Columns.Contains(nameColumn) Then dt.Columns.Add(nameColumn, GetType(String))
        Dim first As DataRow = dt.NewRow()
        first(nameColumn) = firstCaption
        dt.Rows.Add(first)
        For Each r As DataRow In source.Rows
            dt.ImportRow(r)
        Next
        cmb.DropDownStyle = ComboBoxStyle.DropDownList
        cmb.DataSource = dt
        cmb.DisplayMember = nameColumn
        cmb.ValueMember = idColumn
        If cmb.Items.Count > 0 Then cmb.SelectedIndex = 0
    End Sub

    Public Shared Function SelectedId(cmb As ComboBox) As Integer?
        Dim v As Object = cmb.SelectedValue
        If v Is Nothing OrElse v Is DBNull.Value Then Return Nothing
        Return Convert.ToInt32(v)
    End Function

    Public Shared Sub SelectId(cmb As ComboBox, id As Integer?)
        If id.HasValue Then
            cmb.SelectedValue = id.Value
            If cmb.SelectedIndex < 0 Then cmb.SelectedIndex = 0
        Else
            cmb.SelectedIndex = 0
        End If
    End Sub

    ''' <summary>Active rows plus the row currently assigned (so editing an old record never drops its value).</summary>
    Public Shared Function ActiveOrCurrent(source As DataTable, idColumn As String, currentId As Integer?) As DataTable
        Dim dt As DataTable = source.Clone()
        For Each r As DataRow In source.Rows
            Dim isCurrent As Boolean = currentId.HasValue AndAlso Not r.IsNull(idColumn) AndAlso Convert.ToInt32(r(idColumn)) = currentId.Value
            If Convert.ToBoolean(r("IsActive")) OrElse isCurrent Then dt.ImportRow(r)
        Next
        Return dt
    End Function

    ' ---------- state / district ----------
    ''' <summary>Loads states into cmbState and keeps cmbDistrict in step with the chosen state.</summary>
    Public Shared Sub WireStateDistrict(cmbState As ComboBox, cmbDistrict As ComboBox)
        BindLookup(cmbState, AppServices.Reference.States(), "StateID", "Name", "(select state)")
        BindLookup(cmbDistrict, EmptyDistrictTable(), "DistrictID", "Name", "(select district)")
        AddHandler cmbState.SelectedIndexChanged,
            Sub(s As Object, e As EventArgs)
                Dim stateId As Integer? = SelectedId(cmbState)
                Dim t As DataTable = If(stateId.HasValue, AppServices.Reference.Districts(stateId.Value), EmptyDistrictTable())
                If t.Columns.Count = 0 Then
                    t.Columns.Add("DistrictID", GetType(Integer))
                    t.Columns.Add("Name", GetType(String))
                End If
                BindLookup(cmbDistrict, t, "DistrictID", "Name", "(select district)")
            End Sub
    End Sub

    ''' <summary>Empty table with the columns BindLookup needs, used before a state is chosen.</summary>
    Private Shared Function EmptyDistrictTable() As DataTable
        Dim t As New DataTable()
        t.Columns.Add("DistrictID", GetType(Integer))
        t.Columns.Add("Name", GetType(String))
        Return t
    End Function

    Public Shared Sub SetStateDistrict(cmbState As ComboBox, cmbDistrict As ComboBox, stateId As Integer?, districtId As Integer?)
        SelectId(cmbState, stateId)
        SelectId(cmbDistrict, districtId)
    End Sub

    ' ---------- dates ----------
    Public Shared Function MakeDate(Optional allowEmpty As Boolean = False) As DateTimePicker
        Dim d As New DateTimePicker()
        d.Format = DateTimePickerFormat.Custom
        d.CustomFormat = "dd-MM-yyyy"
        d.ShowCheckBox = allowEmpty
        d.Checked = Not allowEmpty
        d.MinDate = New Date(1900, 1, 1)
        d.MaxDate = New Date(2100, 12, 31)
        Return d
    End Function

    ''' <summary>Nothing when an optional picker is unticked.</summary>
    Public Shared Function DateValue(d As DateTimePicker) As Date?
        If d.ShowCheckBox AndAlso Not d.Checked Then Return Nothing
        Return d.Value.Date
    End Function

    Public Shared Sub SetDate(d As DateTimePicker, value As Date?)
        If value.HasValue Then
            d.Value = value.Value
            d.Checked = True
        Else
            d.Checked = False
        End If
    End Sub

    ''' <summary>Text of a box, or Nothing when blank.</summary>
    Public Shared Function TextOrNothing(t As TextBox) As String
        Dim v As String = t.Text.Trim()
        Return If(v.Length = 0, Nothing, v)
    End Function
End Class