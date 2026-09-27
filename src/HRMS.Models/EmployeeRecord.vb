Option Strict On
Option Explicit On

Imports System

Public NotInheritable Class EmployeeRecord
    Public Property EmployeeID As Integer
    Public Property CompanyID As Integer
    Public Property EmpCode As String
    Public Property Name As String
    Public Property FatherName As String
    Public Property DOB As Date?
    ''' <summary>M, F or O</summary>
    Public Property Gender As String
    Public Property DOJ As Date
    Public Property DOL As Date?
    Public Property FactoryID As Integer?
    Public Property DeptID As Integer?
    Public Property DesigID As Integer?
    Public Property SkillCategory As String
    Public Property IFSC As String
    Public Property Mobile As String
    Public Property Address As String
    Public Property IsActive As Boolean = True
    ''' <summary>True when the database returned the encrypted columns (needs EMPLOYEE_SENSITIVE).</summary>
    Public Property SensitiveVisible As Boolean
    ''' <summary>Whether a value is stored (known without decrypting).</summary>
    Public Property HasAadhaar As Boolean
    Public Property HasPAN As Boolean
    Public Property HasBankAcc As Boolean
    Public Property HasUAN As Boolean
    Public Property HasEsiIP As Boolean
    Public Property Cipher As New EmployeeCipher()
End Class
