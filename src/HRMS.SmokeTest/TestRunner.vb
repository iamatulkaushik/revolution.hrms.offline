Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class TestRunner
    Private Shared _passes As Integer
    Private Shared _failures As Integer

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property Passes As Integer
        Get
            Return _passes
        End Get
    End Property

    Public Shared ReadOnly Property Failures As Integer
        Get
            Return _failures
        End Get
    End Property

    Public Shared Sub Section(title As String)
        Console.WriteLine()
        Console.WriteLine("== " & title & " ==")
    End Sub

    Public Shared Sub Check(name As String, test As Func(Of Boolean))
        Try
            If test() Then
                Pass(name)
            Else
                Fail(name, "returned False")
            End If
        Catch ex As Exception
            Fail(name, ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>Passes only when the action throws an exception of type T.</summary>
    Public Shared Sub Expect(Of T As Exception)(name As String, action As Action)
        Try
            action()
            Fail(name, "no exception thrown (expected " & GetType(T).Name & ")")
        Catch ex As Exception
            If TypeOf ex Is T Then
                Pass(name & "  [" & ex.Message.Split(Convert.ToChar(10))(0).Trim() & "]")
            Else
                Fail(name, "expected " & GetType(T).Name & " but got " & ex.GetType().Name & ": " & ex.Message)
            End If
        End Try
    End Sub

    Public Shared Sub Info(message As String)
        Console.WriteLine("      " & message)
    End Sub

    Private Shared Sub Pass(name As String)
        _passes += 1
        Console.WriteLine("PASS  " & name)
    End Sub

    Private Shared Sub Fail(name As String, reason As String)
        _failures += 1
        Console.WriteLine("FAIL  " & name & "  -> " & reason)
    End Sub
End Class
