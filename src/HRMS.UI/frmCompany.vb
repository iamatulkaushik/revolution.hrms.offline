Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Common
Imports HRMS.Models

''' <summary>Company details for the open company. An Associate with no company yet (or pressing "New company") creates one.</summary>
Friend Class frmCompany
    Inherits BaseForm

    Private ReadOnly lblTitle As Label = UiKit.MakeLabel("Company")
    Private ReadOnly lblHint As Label = UiKit.MakeLabel(String.Empty)
    Private ReadOnly tabs As New TabControl()
    ' basic
    Private ReadOnly txtName As TextBox = UiKit.MakeText()
    Private ReadOnly txtAddress As TextBox = UiKit.MakeText()
    Private ReadOnly txtPan As TextBox = UiKit.MakeText()
    Private ReadOnly txtTan As TextBox = UiKit.MakeText()
    Private ReadOnly txtCin As TextBox = UiKit.MakeText()
    Private ReadOnly txtTagline As TextBox = UiKit.MakeText()
    Private ReadOnly dtpStart As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly dtpShut As DateTimePicker = UiKit.MakeDate(True)
    ' contact
    Private ReadOnly cmbState As New ComboBox()
    Private ReadOnly cmbDistrict As New ComboBox()
    Private ReadOnly txtPin As TextBox = UiKit.MakeText()
    Private ReadOnly txtPhone As TextBox = UiKit.MakeText()
    Private ReadOnly txtPhone2 As TextBox = UiKit.MakeText()
    Private ReadOnly txtMobile As TextBox = UiKit.MakeText()
    Private ReadOnly txtMobile2 As TextBox = UiKit.MakeText()
    Private ReadOnly txtEmail As TextBox = UiKit.MakeText()
    Private ReadOnly txtEmail2 As TextBox = UiKit.MakeText()
    Private ReadOnly txtWebsite As TextBox = UiKit.MakeText()
    ' registrations
    Private ReadOnly txtEpf As TextBox = UiKit.MakeText()
    Private ReadOnly dtpEpf As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtEsi As TextBox = UiKit.MakeText()
    Private ReadOnly dtpEsi As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtLin As TextBox = UiKit.MakeText()
    Private ReadOnly txtShop As TextBox = UiKit.MakeText()
    Private ReadOnly dtpShop As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtGst As TextBox = UiKit.MakeText()
    Private ReadOnly dtpGst As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtLabour As TextBox = UiKit.MakeText()
    Private ReadOnly dtpLabourFrom As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly dtpLabourTo As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtPsara As TextBox = UiKit.MakeText()
    Private ReadOnly dtpPsaraFrom As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly dtpPsaraTo As DateTimePicker = UiKit.MakeDate(True)
    ' bank
    Private ReadOnly cmbBank As New ComboBox()
    Private ReadOnly txtAccount As TextBox = UiKit.MakeText()
    Private ReadOnly txtIfsc As TextBox = UiKit.MakeText()
    Private ReadOnly txtBranch As TextBox = UiKit.MakeText()
    ' buttons
    Private ReadOnly btnSave As Button = UiKit.MakeButton("Save", True)
    Private ReadOnly btnNew As Button = UiKit.MakeButton("New company", False)
    Private ReadOnly btnReload As Button = UiKit.MakeButton("Reload", False)
    Private _creating As Boolean

    Public Sub New()
        Text = "Company"
        ClientSize = New Size(760, 640)

        lblTitle.Font = Theme.TitleFont
        lblTitle.ForeColor = Theme.Primary
        lblTitle.Location = New Point(20, 10)
        Controls.Add(UiKit.MakeHeader("Company", "Details of the open company. These print on challans, registers and reports.", 760, 80))
        'Controls.Add(lblTitle)
        lblHint.ForeColor = Theme.Muted
        lblHint.Location = New Point(22, 48)
        'Controls.Add(lblHint)

        tabs.SetBounds(16, 82, 728, 470)
        tabs.Controls.Add(BuildBasicTab())
        tabs.Controls.Add(BuildContactTab())
        tabs.Controls.Add(BuildRegistrationsTab())
        tabs.Controls.Add(BuildBankTab())
        Controls.Add(tabs)

        btnSave.Location = New Point(16, 560)
        btnNew.Location = New Point(136, 560)
        btnNew.Size = New Size(130, 32)
        btnReload.Location = New Point(280, 560)
        Controls.Add(btnSave)
        Controls.Add(btnNew)
        Controls.Add(btnReload)

        UiKit.WireStateDistrict(cmbState, cmbDistrict)
        UiKit.BindLookup(cmbBank, AppServices.Reference.Banks(), "BankID", "Name", "(not set)")
        UiKit.AttachFocusColor(Me)
        AddHandler btnSave.Click, AddressOf OnSaveClick
        AddHandler btnNew.Click, AddressOf OnNewClick
        AddHandler btnReload.Click, AddressOf OnReloadClick
        LoadCurrent()
    End Sub

    ' ---------- tabs ----------
    Private Shared Function NewTab(title As String) As TabPage
        Dim t As New TabPage(title)
        t.BackColor = Color.White
        t.AutoScroll = True
        Return t
    End Function

    Private Shared Sub Row(t As TabPage, caption As String, ctl As Control, y As Integer, width As Integer)
        UiKit.PlaceField(t, caption, 16, y + 4, ctl, 210, y, width)
    End Sub

    Private Function BuildBasicTab() As TabPage
        Dim t As TabPage = NewTab("Basic")
        txtName.MaxLength = 150
        txtAddress.MaxLength = 300
        txtAddress.Multiline = True
        txtAddress.Height = 56
        txtPan.MaxLength = 10
        txtPan.CharacterCasing = CharacterCasing.Upper
        txtTan.MaxLength = 10
        txtTan.CharacterCasing = CharacterCasing.Upper
        txtCin.MaxLength = 21
        txtTagline.MaxLength = 100
        Row(t, "Company name *", txtName, 16, 440)
        Row(t, "Address", txtAddress, 56, 440)
        Row(t, "PAN", txtPan, 128, 200)
        Row(t, "TAN", txtTan, 164, 200)
        Row(t, "CIN", txtCin, 200, 260)
        Row(t, "Tagline", txtTagline, 236, 440)
        Row(t, "Start date", dtpStart, 272, 160)
        Row(t, "Closing date", dtpShut, 308, 160)
        Return t
    End Function

    Private Function BuildContactTab() As TabPage
        Dim t As TabPage = NewTab("Contact")
        txtPin.MaxLength = 6
        txtPhone.MaxLength = 15
        txtPhone2.MaxLength = 15
        txtMobile.MaxLength = 10
        txtMobile2.MaxLength = 10
        txtEmail.MaxLength = 80
        txtEmail2.MaxLength = 80
        txtWebsite.MaxLength = 80
        Row(t, "State", cmbState, 16, 260)
        Row(t, "District", cmbDistrict, 52, 260)
        Row(t, "PIN", txtPin, 88, 100)
        Row(t, "Phone", txtPhone, 124, 180)
        Row(t, "Second phone", txtPhone2, 160, 180)
        Row(t, "Mobile", txtMobile, 196, 180)
        Row(t, "Second mobile", txtMobile2, 232, 180)
        Row(t, "Email", txtEmail, 268, 320)
        Row(t, "Second email", txtEmail2, 304, 320)
        Row(t, "Website", txtWebsite, 340, 320)
        Return t
    End Function

    Private Function BuildRegistrationsTab() As TabPage
        Dim t As TabPage = NewTab("Registrations")
        For Each h As KeyValuePair(Of String, Integer) In New KeyValuePair(Of String, Integer)() {
                New KeyValuePair(Of String, Integer)("Number", 210),
                New KeyValuePair(Of String, Integer)("Date / valid from", 424),
                New KeyValuePair(Of String, Integer)("Valid to", 564)}
            Dim l As Label = UiKit.MakeLabel(h.Key)
            l.Font = Theme.SmallFont
            l.ForeColor = Theme.Muted
            l.Location = New Point(h.Value, 10)
            t.Controls.Add(l)
        Next
        txtEpf.MaxLength = 22
        txtEsi.MaxLength = 17
        txtLin.MaxLength = 20
        txtShop.MaxLength = 50
        txtGst.MaxLength = 20
        txtLabour.MaxLength = 50
        txtPsara.MaxLength = 50
        RegRow(t, "EPF code", txtEpf, dtpEpf, Nothing, 30)
        RegRow(t, "ESIC code", txtEsi, dtpEsi, Nothing, 66)
        RegRow(t, "Labour identification no.", txtLin, Nothing, Nothing, 102)
        RegRow(t, "Shops and Establishment", txtShop, dtpShop, Nothing, 138)
        RegRow(t, "GST", txtGst, dtpGst, Nothing, 174)
        RegRow(t, "Labour licence", txtLabour, dtpLabourFrom, dtpLabourTo, 210)
        RegRow(t, "PSARA licence", txtPsara, dtpPsaraFrom, dtpPsaraTo, 246)
        Return t
    End Function

    Private Shared Sub RegRow(t As TabPage, caption As String, number As TextBox, d1 As DateTimePicker, d2 As DateTimePicker, y As Integer)
        Row(t, caption, number, y, 200)
        If d1 IsNot Nothing Then
            d1.Location = New Point(424, y)
            d1.Width = 130
            t.Controls.Add(d1)
        End If
        If d2 IsNot Nothing Then
            d2.Location = New Point(564, y)
            d2.Width = 130
            t.Controls.Add(d2)
        End If
    End Sub

    Private Function BuildBankTab() As TabPage
        Dim t As TabPage = NewTab("Bank")
        txtAccount.MaxLength = 20
        txtIfsc.MaxLength = 11
        txtIfsc.CharacterCasing = CharacterCasing.Upper
        txtBranch.MaxLength = 250
        Row(t, "Bank", cmbBank, 16, 320)
        Row(t, "Account number", txtAccount, 52, 220)
        Row(t, "IFSC", txtIfsc, 88, 160)
        Row(t, "Branch address", txtBranch, 124, 440)
        Return t
    End Function

    ' ---------- load / save ----------
    Private Sub LoadCurrent()
        Dim s As UserSession = AppSession.Require()
        _creating = Not s.CompanyID.HasValue
        btnSave.Enabled = s.Has("COMPANY_EDIT")
        btnNew.Visible = s.UserType = "Associate" AndAlso s.Has("COMPANY_EDIT") AndAlso Not _creating
        If _creating Then
            ClearAll()
            lblTitle.Text = "New company"
            lblHint.Text = "No company is open yet. Fill the details and Save to create it."
            Return
        End If
        Try
            Dim c As CompanyInfo = AppServices.Company.GetCurrent()
            Dim d As CompanyDetail = AppServices.Company.GetDetail()
            txtName.Text = c.Name
            txtAddress.Text = c.Address
            txtPan.Text = c.PAN
            txtTan.Text = c.TAN
            txtEsi.Text = c.EsiCode
            txtEpf.Text = c.EpfCode
            txtLin.Text = c.LIN
            txtCin.Text = d.CIN
            txtTagline.Text = d.Tagline
            UiKit.SetDate(dtpStart, d.StartDate)
            UiKit.SetDate(dtpShut, d.ShutDate)
            UiKit.SetStateDistrict(cmbState, cmbDistrict, d.StateID, d.DistrictID)
            txtPin.Text = d.PIN
            txtPhone.Text = d.Phone
            txtPhone2.Text = d.Phone2
            txtMobile.Text = d.Mobile
            txtMobile2.Text = d.Mobile2
            txtEmail.Text = d.Email
            txtEmail2.Text = d.Email2
            txtWebsite.Text = d.Website
            UiKit.SetDate(dtpEpf, d.EpfoDate)
            UiKit.SetDate(dtpEsi, d.EsicDate)
            txtShop.Text = d.ShopActNo
            UiKit.SetDate(dtpShop, d.ShopActDate)
            txtGst.Text = d.GstNo
            UiKit.SetDate(dtpGst, d.GstDate)
            txtLabour.Text = d.LabourLicenseNo
            UiKit.SetDate(dtpLabourFrom, d.LabourFrom)
            UiKit.SetDate(dtpLabourTo, d.LabourTo)
            txtPsara.Text = d.PsaraNo
            UiKit.SetDate(dtpPsaraFrom, d.PsaraFrom)
            UiKit.SetDate(dtpPsaraTo, d.PsaraTo)
            UiKit.SelectId(cmbBank, d.BankID)
            txtAccount.Text = d.BankAccount
            txtIfsc.Text = d.BankIfsc
            txtBranch.Text = d.BankBranchAddress
            lblTitle.Text = "Company"
            lblHint.Text = "These details print on challans, registers and reports."
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub ClearAll()
        For Each t As TextBox In New TextBox() {txtName, txtAddress, txtPan, txtTan, txtCin, txtTagline, txtPin, txtPhone, txtPhone2,
                txtMobile, txtMobile2, txtEmail, txtEmail2, txtWebsite, txtEpf, txtEsi, txtLin, txtShop, txtGst, txtLabour, txtPsara,
                txtAccount, txtIfsc, txtBranch}
            t.Clear()
        Next
        For Each d As DateTimePicker In New DateTimePicker() {dtpStart, dtpShut, dtpEpf, dtpEsi, dtpShop, dtpGst, dtpLabourFrom,
                dtpLabourTo, dtpPsaraFrom, dtpPsaraTo}
            d.Checked = False
        Next
        UiKit.SetStateDistrict(cmbState, cmbDistrict, Nothing, Nothing)
        UiKit.SelectId(cmbBank, Nothing)
    End Sub

    Private Function ReadBasic() As CompanyInfo
        Return New CompanyInfo With {
            .Name = txtName.Text.Trim(), .Address = UiKit.TextOrNothing(txtAddress), .PAN = UiKit.TextOrNothing(txtPan),
            .TAN = UiKit.TextOrNothing(txtTan), .EsiCode = UiKit.TextOrNothing(txtEsi),
            .EpfCode = UiKit.TextOrNothing(txtEpf), .LIN = UiKit.TextOrNothing(txtLin)}
    End Function

    Private Function ReadDetail() As CompanyDetail
        Return New CompanyDetail With {
            .Tagline = UiKit.TextOrNothing(txtTagline), .StartDate = UiKit.DateValue(dtpStart), .ShutDate = UiKit.DateValue(dtpShut),
            .StateID = UiKit.SelectedId(cmbState), .DistrictID = UiKit.SelectedId(cmbDistrict), .PIN = UiKit.TextOrNothing(txtPin),
            .Phone = UiKit.TextOrNothing(txtPhone), .Phone2 = UiKit.TextOrNothing(txtPhone2),
            .Mobile = UiKit.TextOrNothing(txtMobile), .Mobile2 = UiKit.TextOrNothing(txtMobile2),
            .Email = UiKit.TextOrNothing(txtEmail), .Email2 = UiKit.TextOrNothing(txtEmail2), .Website = UiKit.TextOrNothing(txtWebsite),
            .CIN = UiKit.TextOrNothing(txtCin), .ShopActNo = UiKit.TextOrNothing(txtShop), .ShopActDate = UiKit.DateValue(dtpShop),
            .GstNo = UiKit.TextOrNothing(txtGst), .GstDate = UiKit.DateValue(dtpGst),
            .EpfoDate = UiKit.DateValue(dtpEpf), .EsicDate = UiKit.DateValue(dtpEsi),
            .LabourLicenseNo = UiKit.TextOrNothing(txtLabour), .LabourFrom = UiKit.DateValue(dtpLabourFrom), .LabourTo = UiKit.DateValue(dtpLabourTo),
            .PsaraNo = UiKit.TextOrNothing(txtPsara), .PsaraFrom = UiKit.DateValue(dtpPsaraFrom), .PsaraTo = UiKit.DateValue(dtpPsaraTo),
            .BankID = UiKit.SelectedId(cmbBank), .BankAccount = UiKit.TextOrNothing(txtAccount),
            .BankIfsc = UiKit.TextOrNothing(txtIfsc), .BankBranchAddress = UiKit.TextOrNothing(txtBranch)}
    End Function

    Private Sub OnSaveClick(sender As Object, e As EventArgs)
        Try
            Dim basic As CompanyInfo = ReadBasic()
            Dim detail As CompanyDetail = ReadDetail()
            Dim problems = CompanyService.ValidateDetail(detail)
            If problems.Count > 0 Then Throw New BusinessException(String.Join(Environment.NewLine, problems))

            If _creating Then
                Dim newId As Integer = AppServices.Company.Create(basic)
                AppServices.Auth.CompleteLogin(newId)
                AppServices.Company.SaveDetail(detail)
                AppServices.RaiseSessionChanged()
                LoadCurrent()
                ShowInfo("Company created and opened.")
            Else
                AppServices.Company.Update(basic)
                AppServices.Company.SaveDetail(detail)
                ShowInfo("Saved.")
            End If
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Private Sub OnNewClick(sender As Object, e As EventArgs)
        _creating = True
        ClearAll()
        btnNew.Visible = False
        lblTitle.Text = "New company"
        lblHint.Text = "The current company stays open until you Save. Reload cancels."
        tabs.SelectedIndex = 0
        txtName.Focus()
    End Sub

    Private Sub OnReloadClick(sender As Object, e As EventArgs)
        LoadCurrent()
    End Sub
End Class
