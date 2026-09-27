Option Strict On
Option Explicit On

Imports System.Security.Cryptography
Imports System.Text

''' <summary>Windows DPAPI. LocalMachine = any local account on this PC can unwrap; CurrentUser = only this Windows account.</summary>
Public NotInheritable Class DpapiKeyProtector
    Implements IKeyProtector

    Private Shared ReadOnly ENTROPY As Byte() = Encoding.UTF8.GetBytes("RevolutionHRMS.v1")
    Private ReadOnly _scope As DataProtectionScope

    Public Sub New()
        _scope = DataProtectionScope.LocalMachine
    End Sub

    Public Sub New(scope As DataProtectionScope)
        _scope = scope
    End Sub

    Public Function Protect(data As Byte()) As Byte() Implements IKeyProtector.Protect
        Return ProtectedData.Protect(data, ENTROPY, _scope)
    End Function

    Public Function Unprotect(data As Byte()) As Byte() Implements IKeyProtector.Unprotect
        Return ProtectedData.Unprotect(data, ENTROPY, _scope)
    End Function
End Class
