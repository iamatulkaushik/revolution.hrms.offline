Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports HRMS.Common
Imports HRMS.Models

''' <summary>Family, contact, present/permanent address and education (needed for the statutory registers).</summary>
Friend Class ucEmployeeMore
    Inherits UserControl

    Private ReadOnly txtMother As TextBox = UiKit.MakeText()
    Private ReadOnly txtBirthPlace As TextBox = UiKit.MakeText()
    Private ReadOnly cmbMarital As New ComboBox()
    Private ReadOnly txtHeight As TextBox = UiKit.MakeText()
    Private ReadOnly txtMark As TextBox = UiKit.MakeText()
    Private ReadOnly txtMobile2 As TextBox = UiKit.MakeText()
    Private ReadOnly txtEmail As TextBox = UiKit.MakeText()
    Private ReadOnly txtEmail2 As TextBox = UiKit.MakeText()
    Private ReadOnly cmbPState As New ComboBox()
    Private ReadOnly cmbPDistrict As New ComboBox()
    Private ReadOnly txtPPin As TextBox = UiKit.MakeText()
    Private ReadOnly txtPermAddress As TextBox = UiKit.MakeText()
    Private ReadOnly cmbMState As New ComboBox()
    Private ReadOnly cmbMDistrict As New ComboBox()
    Private ReadOnly txtMPin As TextBox = UiKit.MakeText()
    Private ReadOnly txtEduStd As TextBox = UiKit.MakeText()
    Private ReadOnly txtEduBoard As TextBox = UiKit.MakeText()
    Private ReadOnly txtEduYear As TextBox = UiKit.MakeText()
    Private ReadOnly txtEduPercent As TextBox = UiKit.MakeText()
    Private ReadOnly txtEduRemarks As TextBox = UiKit.MakeText()

    Public Sub New()
        BackColor = Color.White
        AutoScroll = True
        cmbMarital.DropDownStyle = ComboBoxStyle.DropDownList
        cmbMarital.Items.AddRange(New Object() {"(not set)", "Unmarried", "Married", "Widowed", "Divorced"})
        cmbMarital.SelectedIndex = 0
        txtMother.MaxLength = 100
        txtBirthPlace.MaxLength = 80
        txtHeight.MaxLength = 10
        txtMark.MaxLength = 150
        txtMobile2.MaxLength = 10
        txtEmail.MaxLength = 80
        txtEmail2.MaxLength = 80
        txtPPin.MaxLength = 6
        txtMPin.MaxLength = 6
        txtPermAddress.MaxLength = 300
        txtPermAddress.Multiline = True
        txtPermAddress.Height = 56
        txtEduStd.MaxLength = 150
        txtEduBoard.MaxLength = 250
        txtEduYear.MaxLength = 50
        txtEduPercent.MaxLength = 6
        txtEduRemarks.MaxLength = 250

        Dim y As Integer = 12
        Row("Mother's name", txtMother, 300, y)
        Row("Place of birth", txtBirthPlace, 300, y)
        Row("Marital status", cmbMarital, 160, y)
        Row("Height", txtHeight, 100, y)
        Row("Identification mark", txtMark, 400, y)
        Row("Second mobile", txtMobile2, 140, y)
        Row("Email", txtEmail, 300, y)
        Row("Second email", txtEmail2, 300, y)
        Heading("Present address (street is on the Personal tab)", y)
        Row("State", cmbPState, 240, y)
        Row("District", cmbPDistrict, 240, y)
        Row("PIN", txtPPin, 100, y)
        Heading("Permanent address", y)
        Row("Address", txtPermAddress, 400, y, 62)
        Row("State", cmbMState, 240, y)
        Row("District", cmbMDistrict, 240, y)
        Row("PIN", txtMPin, 100, y)
        Heading("Education", y)
        Row("Standard / qualification", txtEduStd, 400, y)
        Row("Board / university", txtEduBoard, 400, y)
        Row("Year of passing", txtEduYear, 140, y)
        Row("Percentage / CGPA", txtEduPercent, 100, y)
        Row("Remarks", txtEduRemarks, 400, y)
        UiKit.WireStateDistrict(cmbPState, cmbPDistrict)
        UiKit.WireStateDistrict(cmbMState, cmbMDistrict)
    End Sub

    Private Sub Row(caption As String, ctl As Control, width As Integer, ByRef y As Integer, Optional rowHeight As Integer = 34)
        UiKit.PlaceField(Me, caption, 16, y + 4, ctl, 210, y, width)
        y += rowHeight
    End Sub

    Private Sub Heading(text As String, ByRef y As Integer)
        Dim l As Label = UiKit.MakeLabel(text)
        l.Font = New Font(Theme.BaseFont, FontStyle.Bold)
        l.ForeColor = Theme.Primary
        l.Location = New Point(16, y + 8)
        Controls.Add(l)
        y += 38
    End Sub

    Public Sub SetReadOnly(readOnlyMode As Boolean)
        For Each t As TextBox In New TextBox() {txtMother, txtBirthPlace, txtHeight, txtMark, txtMobile2, txtEmail, txtEmail2,
                txtPPin, txtPermAddress, txtMPin, txtEduStd, txtEduBoard, txtEduYear, txtEduPercent, txtEduRemarks}
            t.ReadOnly = readOnlyMode
        Next
        For Each c As ComboBox In New ComboBox() {cmbMarital, cmbPState, cmbPDistrict, cmbMState, cmbMDistrict}
            c.Enabled = Not readOnlyMode
        Next
    End Sub

    Public Sub LoadFrom(d As EmployeeDetail)
        txtMother.Text = d.MotherName
        txtBirthPlace.Text = d.BirthPlace
        cmbMarital.SelectedIndex = "UMWD".IndexOf(If(d.MaritalStatus, "?")) + 1
        txtHeight.Text = d.Height
        txtMark.Text = d.IdentityMark
        txtMobile2.Text = d.Mobile2
        txtEmail.Text = d.Email
        txtEmail2.Text = d.Email2
        UiKit.SetStateDistrict(cmbPState, cmbPDistrict, d.PresentStateID, d.PresentDistrictID)
        txtPPin.Text = d.PresentPIN
        txtPermAddress.Text = d.PermAddress
        UiKit.SetStateDistrict(cmbMState, cmbMDistrict, d.PermStateID, d.PermDistrictID)
        txtMPin.Text = d.PermPIN
        txtEduStd.Text = d.EduStandard
        txtEduBoard.Text = d.EduBoard
        txtEduYear.Text = d.EduPassYear
        txtEduPercent.Text = If(d.EduPercent.HasValue, d.EduPercent.Value.ToString("0.##", CultureInfo.InvariantCulture), String.Empty)
        txtEduRemarks.Text = d.EduRemarks
    End Sub

    ''' <summary>Copies the boxes into d (the statutory tab fills the other half).</summary>
    Public Sub ReadInto(d As EmployeeDetail)
        d.MotherName = UiKit.TextOrNothing(txtMother)
        d.BirthPlace = UiKit.TextOrNothing(txtBirthPlace)
        d.MaritalStatus = If(cmbMarital.SelectedIndex <= 0, Nothing, "UMWD".Substring(cmbMarital.SelectedIndex - 1, 1))
        d.Height = UiKit.TextOrNothing(txtHeight)
        d.IdentityMark = UiKit.TextOrNothing(txtMark)
        d.Mobile2 = UiKit.TextOrNothing(txtMobile2)
        d.Email = UiKit.TextOrNothing(txtEmail)
        d.Email2 = UiKit.TextOrNothing(txtEmail2)
        d.PresentStateID = UiKit.SelectedId(cmbPState)
        d.PresentDistrictID = UiKit.SelectedId(cmbPDistrict)
        d.PresentPIN = UiKit.TextOrNothing(txtPPin)
        d.PermAddress = UiKit.TextOrNothing(txtPermAddress)
        d.PermStateID = UiKit.SelectedId(cmbMState)
        d.PermDistrictID = UiKit.SelectedId(cmbMDistrict)
        d.PermPIN = UiKit.TextOrNothing(txtMPin)
        d.EduStandard = UiKit.TextOrNothing(txtEduStd)
        d.EduBoard = UiKit.TextOrNothing(txtEduBoard)
        d.EduPassYear = UiKit.TextOrNothing(txtEduYear)
        d.EduRemarks = UiKit.TextOrNothing(txtEduRemarks)
        d.EduPercent = Nothing
        Dim pct As String = txtEduPercent.Text.Trim()
        If pct.Length > 0 Then
            Dim v As Decimal
            If Not Decimal.TryParse(pct, NumberStyles.Number, CultureInfo.InvariantCulture, v) Then
                Throw New BusinessException("Percentage / CGPA must be a number.")
            End If
            d.EduPercent = v
        End If
    End Sub
End Class
