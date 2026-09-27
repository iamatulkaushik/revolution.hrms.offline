Option Strict On
Option Explicit On

Imports System
Imports System.IO

''' <summary>Runtime folders. Override root with env var HRMS_DATA_DIR (dev/tests).</summary>
Public NotInheritable Class AppPaths
    Private Sub New()
    End Sub

    Public Shared ReadOnly Property DataDir As String
        Get
            Dim overrideDir As String = Environment.GetEnvironmentVariable("HRMS_DATA_DIR")
            If Not String.IsNullOrWhiteSpace(overrideDir) Then Return overrideDir
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RevolutionHRMS")
        End Get
    End Property

    Public Shared ReadOnly Property LogDir As String
        Get
            Return Path.Combine(DataDir, "Logs")
        End Get
    End Property

    Public Shared ReadOnly Property KeyDir As String
        Get
            Return Path.Combine(DataDir, "Keys")
        End Get
    End Property

    Public Shared ReadOnly Property ConfigDir As String
        Get
            Return Path.Combine(DataDir, "Config")
        End Get
    End Property
End Class
