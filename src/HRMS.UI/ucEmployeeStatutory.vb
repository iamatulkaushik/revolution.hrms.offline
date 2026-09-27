Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports HRMS.Models

''' <summary>EPF / ESIC / labour details, names on documents, bank, payment and employment type.</summary>
Friend Class ucEmployeeStatutory
    Inherits UserControl

    Private ReadOnly dtpUanDoj As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtMemberId As TextBox = UiKit.MakeText()
    Private ReadOnly chkHigher As New CheckBox()
    Private ReadOnly dtpEsicDoj As DateTimePicker = UiKit.MakeDate(True)
    Private ReadOnly txtEsicReason As TextBox = UiKit.MakeText()
    Private ReadOnly txtLabourId As TextBox = UiKit.MakeText()
    Private ReadOnly txtPanName As TextBox = UiKit.MakeText()
    Private ReadOnly txtAadhaarName As TextBox = UiKit.MakeText()
    Private ReadOnly cmbBank As New ComboBox()
    Private ReadOnly cmbPayment As New ComboBox()
    Private ReadOnly cmbEmployment As New ComboBox()

    Public Sub New()
        BackColor = Color.White
        AutoScroll = True
        txtMemberId.MaxLength = 10
        txtEsicReason.MaxLength = 100
        txtLabourId.MaxLength = 10
        txtPanName.MaxLength = 50
        txtAadhaarName.MaxLength = 50
        chkHigher.Text = "Contributes on wage above the EPF ceiling"
        chkHigher.AutoSize = True
        cmbPayment.DropDownStyle = ComboBoxStyle.DropDownList
        cmbPayment.Items.AddRange(New Object() {"Bank transfer", "Cash", "Cheque"})
        cmbPayment.SelectedIndex = 0
        cmbEmployment.DropDownStyle = ComboBoxStyle.DropDownList
        cmbEmployment.Items.AddRange(New Object() {"Regular", "Contract", "Casual / daily wage", "Trainee"})
        cmbEmployment.SelectedIndex = 0
        UiKit.BindLookup(cmbBank, AppServices.Reference.Banks(), "BankID", "Name", "(not set)")

        Heading("EPF", 12)
        Row("UAN date of joining", dtpUanDoj, 160, 50)
        Row("EPF member (passbook) ID", txtMemberId, 160, 84)
        chkHigher.Location = New Point(210, 120)
        Controls.Add(chkHigher)
        Heading("ESIC and labour", 156)
        Row("ESIC date of joining", dtpEsicDoj, 160, 194)
        Row("ESIC exit reason", txtEsicReason, 300, 228)
        Row("Labour ID", txtLabourId, 160, 262)
        Heading("Names as printed on documents", 298)
        Row("Name on PAN", txtPanName, 300, 336)
        Row("Name on Aadhaar", txtAadhaarName, 300, 370)
        Heading("Pay and employment", 406)
        Row("Bank", cmbBank, 300, 444)
        Row("Payment mode", cmbPayment, 200, 478)
        Row("Employment type", cmbEmployment, 200, 512)
        Dim note As Label = UiKit.MakeLabel("The account number and IFSC are on the IDs and bank tab.")
        note.Font = Theme.SmallFont
        note.ForeColor = Theme.Muted
        note.Location = New Point(210, 548)
        Controls.Add(note)
    End Sub

    Private Sub Row(caption As String, ctl As Control, width As Integer, y As Integer)
        UiKit.PlaceField(Me, caption, 16, y + 4, ctl, 210, y, width)
    End Sub

    Private Sub Heading(text As String, y As Integer)
        Dim l As Label = UiKit.MakeLabel(text)
        l.Font = New Font(Theme.BaseFont, FontStyle.Bold)
        l.ForeColor = Theme.Primary
        l.Location = New Point(16, y + 4)
        Controls.Add(l)
    End Sub

    Public Sub SetReadOnly(readOnlyMode As Boolean)
        For Each t As TextBox In New TextBox() {txtMemberId, txtEsicReason, txtLabourId, txtPanName, txtAadhaarName}
            t.ReadOnly = readOnlyMode
        Next
        For Each c As Control In New Control() {dtpUanDoj, dtpEsicDoj, chkHigher, cmbBank, cmbPayment, cmbEmployment}
            c.Enabled = Not readOnlyMode
        Next
    End Sub

    Public Sub LoadFrom(d As EmployeeDetail)
        UiKit.SetDate(dtpUanDoj, d.UanDoj)
        txtMemberId.Text = d.EpfMemberId
        chkHigher.Checked = d.EpfHigher
        UiKit.SetDate(dtpEsicDoj, d.EsicDoj)
        txtEsicReason.Text = d.EsicExitReason
        txtLabourId.Text = d.LabourId
        txtPanName.Text = d.PanName
        txtAadhaarName.Text = d.AadhaarName
        UiKit.SelectId(cmbBank, d.BankID)
        cmbPayment.SelectedIndex = Math.Max(0, Math.Min(cmbPayment.Items.Count - 1, d.PaymentType - 1))
        cmbEmployment.SelectedIndex = Math.Max(0, Math.Min(cmbEmployment.Items.Count - 1, d.EmploymentType - 1))
    End Sub

    Public Sub ReadInto(d As EmployeeDetail)
        d.UanDoj = UiKit.DateValue(dtpUanDoj)
        d.EpfMemberId = UiKit.TextOrNothing(txtMemberId)
        d.EpfHigher = chkHigher.Checked
        d.EsicDoj = UiKit.DateValue(dtpEsicDoj)
        d.EsicExitReason = UiKit.TextOrNothing(txtEsicReason)
        d.LabourId = UiKit.TextOrNothing(txtLabourId)
        d.PanName = UiKit.TextOrNothing(txtPanName)
        d.AadhaarName = UiKit.TextOrNothing(txtAadhaarName)
        d.BankID = UiKit.SelectedId(cmbBank)
        d.PaymentType = cmbPayment.SelectedIndex + 1
        d.EmploymentType = cmbEmployment.SelectedIndex + 1
    End Sub
End Class
