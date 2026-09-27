Option Strict On
Option Explicit On

Imports System
Imports System.Text
Imports HRMS.Common

''' <summary>Usage:
'''   HRMS.SmokeTest                 offline self-tests only (no database)
'''   HRMS.SmokeTest --db            also run database tests
''' Environment:
'''   HRMS_CONN        connection string (hrms_app login), required for --db
'''   HRMS_ADMIN_PWD   admin password (used to bootstrap on a fresh DB, or to log in); prompted if absent
'''   HRMS_DATA_DIR    optional: use a different folder for keys/logs</summary>
Public Module Program
    Public Function Main(args As String()) As Integer
        Console.WriteLine("Revolution HRMS smoke test")
        SelfTests.Run()

        If Array.IndexOf(args, "--db") >= 0 Then
            Dim cs As String = Environment.GetEnvironmentVariable(ConnectionStringStore.ENV_VAR)
            If String.IsNullOrWhiteSpace(cs) Then
                Console.WriteLine()
                Console.WriteLine("Set " & ConnectionStringStore.ENV_VAR & " to the hrms_app connection string, e.g.:")
                Console.WriteLine("  Server=.\SQLEXPRESS;Database=HRMS_Data;User Id=hrms_app;Password=***;Encrypt=True;TrustServerCertificate=True")
                Return 2
            End If
            Dim pwd As String = Environment.GetEnvironmentVariable("HRMS_ADMIN_PWD")
            If String.IsNullOrEmpty(pwd) Then pwd = ReadPassword("Admin password (new DB: choose one, 8+ chars with letters and digits): ")

            Dim keys As New KeyStore(New DpapiKeyProtector(), KeyStore.DefaultPath())
            If Not keys.Exists() Then keys.CreateNew()
            DbTests.Run(cs, New FieldCrypto(keys.Load()), pwd)
        End If

        Console.WriteLine()
        Console.WriteLine("Result: " & TestRunner.Passes & " passed, " & TestRunner.Failures & " failed")
        Return If(TestRunner.Failures = 0, 0, 1)
    End Function

    Private Function ReadPassword(prompt As String) As String
        Console.Write(prompt)
        Dim sb As New StringBuilder()
        Do
            Dim k As ConsoleKeyInfo = Console.ReadKey(True)
            If k.Key = ConsoleKey.Enter Then Exit Do
            If k.Key = ConsoleKey.Backspace Then
                If sb.Length > 0 Then sb.Length -= 1
            ElseIf Not Char.IsControl(k.KeyChar) Then
                sb.Append(k.KeyChar)
            End If
        Loop
        Console.WriteLine()
        Return sb.ToString()
    End Function
End Module
