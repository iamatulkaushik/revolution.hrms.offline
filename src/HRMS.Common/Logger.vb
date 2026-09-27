Option Strict On
Option Explicit On

Imports System
Imports System.Globalization
Imports System.IO

''' <summary>Rolling daily file log. Never log passwords, Aadhaar, PAN, bank numbers.</summary>
Public NotInheritable Class Logger
    Private Shared ReadOnly _sync As New Object()

    Private Sub New()
    End Sub

    Public Shared Sub Info(message As String)
        Write("INFO", message)
    End Sub

    Public Shared Sub Warn(message As String)
        Write("WARN", message)
    End Sub

    Public Shared Sub Failure(context As String, ex As Exception)
        If ex Is Nothing Then
            Write("ERROR", context)
        Else
            Write("ERROR", context & " | " & ex.GetType().Name & ": " & ex.Message & Environment.NewLine & ex.StackTrace)
        End If
    End Sub

    Private Shared Sub Write(level As String, message As String)
        Try
            SyncLock _sync
                Directory.CreateDirectory(AppPaths.LogDir)
                Dim logPath As String = Path.Combine(AppPaths.LogDir, "hrms-" & Date.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) & ".log")
                Dim stamp As String = Date.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                File.AppendAllText(logPath, stamp & " [" & level & "] " & message & Environment.NewLine)
            End SyncLock
        Catch ex As IOException
            ' logging must never crash the app
        Catch ex As UnauthorizedAccessException
            ' logging must never crash the app
        End Try
    End Sub
End Class
