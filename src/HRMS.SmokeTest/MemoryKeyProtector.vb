Option Strict On
Option Explicit On

Imports System
Imports System.Security.Cryptography
Imports HRMS.Common

''' <summary>Test-only key wrapper (random per-instance keys). Production uses DpapiKeyProtector.</summary>
Public NotInheritable Class MemoryKeyProtector
    Implements IKeyProtector

    Private ReadOnly _enc(31) As Byte
    Private ReadOnly _mac(31) As Byte
    Private Shared ReadOnly CONTEXT As Byte() = New Byte() {1, 2, 3}

    Public Sub New()
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(_enc)
            rng.GetBytes(_mac)
        End Using
    End Sub

    Public Function Protect(data As Byte()) As Byte() Implements IKeyProtector.Protect
        Return AuthCrypto.Encrypt(_enc, _mac, data, CONTEXT)
    End Function

    Public Function Unprotect(data As Byte()) As Byte() Implements IKeyProtector.Unprotect
        Return AuthCrypto.Decrypt(_enc, _mac, data, CONTEXT)
    End Function
End Class
