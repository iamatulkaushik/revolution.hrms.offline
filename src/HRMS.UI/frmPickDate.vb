Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Small dialog that asks for one date (e.g. leaving date).</summary>
Friend Class frmPickDate
    Inherits BaseForm

    Private ReadOnly dtp As DateTimePicker = UiKit.MakeDate()
    Private ReadOnly btnOk As Button = UiKit.MakeButton("OK", True)
    Private ReadOnly btnCancel As Button = UiKit.MakeButton("Cancel", False)

    Public ReadOnly Property Value As Date
        Get
            Return dtp.Value.Date
        End Get
    End Property

    Public Sub New(title As String, prompt As String, initial As Date)
        Text = ErrorHandler.APP_TITLE & " - " & title
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        ClientSize = New Size(360, 180)
        Controls.Add(UiKit.MakeHeader(title, Nothing, 360, 56))
        UiKit.PlaceField(Me, prompt, 24, 70, dtp, 24, 92, 160)
        dtp.Value = initial
        btnOk.Location = New Point(112, 130)
        btnCancel.Location = New Point(232, 130)
        Controls.Add(btnOk)
        Controls.Add(btnCancel)
        AcceptButton = btnOk
        CancelButton = btnCancel
        btnOk.DialogResult = DialogResult.OK
        btnCancel.DialogResult = DialogResult.Cancel
    End Sub
End Class
