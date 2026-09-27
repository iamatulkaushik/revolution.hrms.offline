Option Strict On
Option Explicit On

Imports System

''' <summary>Encrypted sensitive columns exactly as stored. Nothing = no value / unchanged.</summary>
Public NotInheritable Class EmployeeCipher
    Public Property AadhaarEnc As Byte()
    Public Property PANEnc As Byte()
    Public Property BankAccEnc As Byte()
    Public Property UANEnc As Byte()
    Public Property EsiIPEnc As Byte()
End Class
