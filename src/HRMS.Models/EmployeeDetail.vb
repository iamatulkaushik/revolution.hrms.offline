Option Strict On
Option Explicit On

Imports System

''' <summary>Less-used employee data: family, address, education, EPF/ESIC/labour details, payment and employment type.</summary>
Public NotInheritable Class EmployeeDetail
    Public Property MotherName As String
    Public Property BirthPlace As String
    ''' <summary>U unmarried, M married, W widowed, D divorced</summary>
    Public Property MaritalStatus As String
    Public Property Height As String
    Public Property IdentityMark As String
    Public Property Mobile2 As String
    Public Property Email As String
    Public Property Email2 As String
    Public Property PresentStateID As Integer?
    Public Property PresentDistrictID As Integer?
    Public Property PresentPIN As String
    Public Property PermAddress As String
    Public Property PermStateID As Integer?
    Public Property PermDistrictID As Integer?
    Public Property PermPIN As String
    Public Property EduStandard As String
    Public Property EduBoard As String
    Public Property EduPassYear As String
    Public Property EduPercent As Decimal?
    Public Property EduRemarks As String
    Public Property UanDoj As Date?
    Public Property EpfMemberId As String
    Public Property EpfHigher As Boolean
    Public Property EsicDoj As Date?
    Public Property EsicExitReason As String
    Public Property LabourId As String
    Public Property PanName As String
    Public Property AadhaarName As String
    Public Property BankID As Integer?
    ''' <summary>1 bank transfer, 2 cash, 3 cheque</summary>
    Public Property PaymentType As Integer = 1
    ''' <summary>1 regular, 2 contract, 3 casual / daily wage, 4 trainee</summary>
    Public Property EmploymentType As Integer = 1
End Class
