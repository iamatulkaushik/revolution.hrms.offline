Option Strict On
Option Explicit On

Imports System.Drawing

''' <summary>Colours and fonts from design.md.</summary>
Friend NotInheritable Class Theme
    Public Shared ReadOnly Primary As Color = ColorTranslator.FromHtml("#1F4E79")
    Public Shared ReadOnly Accent As Color = ColorTranslator.FromHtml("#2E8B57")
    Public Shared ReadOnly Danger As Color = ColorTranslator.FromHtml("#B22222")
    Public Shared ReadOnly Background As Color = ColorTranslator.FromHtml("#F4F6F8")
    Public Shared ReadOnly GridAlt As Color = ColorTranslator.FromHtml("#EEF3F8")
    Public Shared ReadOnly InputFocus As Color = ColorTranslator.FromHtml("#FFF8DC")
    Public Shared ReadOnly Muted As Color = ColorTranslator.FromHtml("#5F6B76")

    Public Shared ReadOnly BaseFont As New Font("Segoe UI", 9.5F)
    Public Shared ReadOnly TitleFont As New Font("Segoe UI", 16.0F, FontStyle.Bold)
    Public Shared ReadOnly SubtitleFont As New Font("Segoe UI", 9.5F)
    Public Shared ReadOnly SmallFont As New Font("Segoe UI", 8.5F)

    Private Sub New()
    End Sub
End Class
