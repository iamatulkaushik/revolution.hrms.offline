Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>Friendly messages for users, full detail in the log (no stack traces on screen).</summary>
Friend NotInheritable Class ErrorHandler
    Public Const APP_TITLE As String = "Revolution HRMS"

    Private Sub New()
    End Sub

    Public Shared Sub Show(owner As IWin32Window, ex As Exception)
        If TypeOf ex Is BusinessException Then
            MessageBox.Show(owner, ex.Message, APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Logger.Failure("Unexpected UI error", ex)
        MessageBox.Show(owner, "An unexpected error occurred. It has been written to the log." & Environment.NewLine &
                        "Please note what you were doing and contact support.", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    Public Shared Sub ShowInfo(owner As IWin32Window, text As String)
        MessageBox.Show(owner, text, APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Shared Function Confirm(owner As IWin32Window, text As String) As Boolean
        Return MessageBox.Show(owner, text, APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
    End Function
End Class
