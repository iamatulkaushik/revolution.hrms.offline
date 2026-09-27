Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class ByteUtil
    Private Sub New()
    End Sub

    Public Shared Function Concat(ParamArray parts As Byte()()) As Byte()
        Dim total As Integer = 0
        For Each p As Byte() In parts
            total += p.Length
        Next
        Dim result(total - 1) As Byte
        Dim pos As Integer = 0
        For Each p As Byte() In parts
            Buffer.BlockCopy(p, 0, result, pos, p.Length)
            pos += p.Length
        Next
        Return result
    End Function

    ''' <summary>Compares without early exit (timing-safe).</summary>
    Public Shared Function ConstantTimeEquals(a As Byte(), b As Byte()) As Boolean
        If a Is Nothing OrElse b Is Nothing Then Return False
        Dim diff As Integer = a.Length Xor b.Length
        Dim n As Integer = Math.Min(a.Length, b.Length)
        For i As Integer = 0 To n - 1
            diff = diff Or (a(i) Xor b(i))
        Next
        Return diff = 0
    End Function

    Public Shared Sub Wipe(data As Byte())
        If data IsNot Nothing Then Array.Clear(data, 0, data.Length)
    End Sub
End Class
