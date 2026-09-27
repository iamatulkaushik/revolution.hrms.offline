Option Strict On
Option Explicit On

''' <summary>Wraps key material at rest. Production: DPAPI. Tests: in-memory.</summary>
Public Interface IKeyProtector
    Function Protect(data As Byte()) As Byte()
    Function Unprotect(data As Byte()) As Byte()
End Interface
