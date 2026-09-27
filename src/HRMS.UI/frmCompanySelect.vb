Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Pick the company to work in. allowNone (Associates): open with no company, e.g. to create the first one.</summary>
Friend Class frmCompanySelect
    Inherits BaseForm

    Private ReadOnly lstCompanies As New ListBox()
    Private ReadOnly btnOpen As Button = UiKit.MakeButton("Open", True)
    Private ReadOnly btnNone As Button = UiKit.MakeButton("No company", False)
    Private ReadOnly btnCancel As Button = UiKit.MakeButton("Log out", False)

    ''' <summary>Nothing = the user chose to continue without a company.</summary>
    Public Property SelectedCompanyID As Integer?

    Public Sub New(companies As DataTable, allowNone As Boolean, cancelCaption As String)
        Text = ErrorHandler.APP_TITLE & " - Select company"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterScreen
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(420, 400)
        EnterMovesFocus = False
        btnCancel.Text = cancelCaption

        Controls.Add(UiKit.MakeHeader("Select company", "Choose the company you want to work on", 420, 80))

        Dim items As New List(Of KeyValuePair(Of Integer, String))()
        For Each row As DataRow In companies.Rows
            items.Add(New KeyValuePair(Of Integer, String)(Convert.ToInt32(row("CompanyID")), Convert.ToString(row("Name"))))
        Next
        lstCompanies.DataSource = items
        lstCompanies.DisplayMember = "Value"
        lstCompanies.ValueMember = "Key"
        lstCompanies.BorderStyle = BorderStyle.FixedSingle
        lstCompanies.IntegralHeight = False
        lstCompanies.SetBounds(28, 100, 364, 220)
        Controls.Add(lstCompanies)

        btnOpen.Location = New Point(28, 338)
        btnNone.Location = New Point(148, 338)
        btnNone.Visible = allowNone
        btnCancel.Location = New Point(282, 338)
        Controls.Add(btnOpen)
        Controls.Add(btnNone)
        Controls.Add(btnCancel)

        btnOpen.Enabled = items.Count > 0
        AcceptButton = btnOpen
        CancelButton = btnCancel
        btnCancel.DialogResult = DialogResult.Cancel
        AddHandler btnOpen.Click, AddressOf OnOpenClick
        AddHandler lstCompanies.DoubleClick, AddressOf OnOpenClick
        AddHandler btnNone.Click, AddressOf OnNoneClick
    End Sub

    Private Sub OnOpenClick(sender As Object, e As EventArgs)
        If lstCompanies.SelectedItem Is Nothing Then Return
        Dim item As KeyValuePair(Of Integer, String) = DirectCast(lstCompanies.SelectedItem, KeyValuePair(Of Integer, String))
        SelectedCompanyID = item.Key
        DialogResult = DialogResult.OK
    End Sub

    Private Sub OnNoneClick(sender As Object, e As EventArgs)
        SelectedCompanyID = Nothing
        DialogResult = DialogResult.OK
    End Sub
End Class
