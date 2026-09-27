Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

''' <summary>Who is logged in and for which company. CompanyID is Nothing for an Associate
''' who has not chosen (or has no) company yet.</summary>
Public NotInheritable Class UserSession
    Private ReadOnly _permissions As HashSet(Of String)

    Public ReadOnly Property UserID As Integer
    Public ReadOnly Property Username As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property RoleID As Integer
    Public ReadOnly Property UserType As String
    Public ReadOnly Property EmployeeID As Integer?
    Public ReadOnly Property CompanyID As Integer?
    Public ReadOnly Property CompanyName As String
    Public Property MustChangePassword As Boolean

    Public Sub New(userID As Integer, username As String, fullName As String, roleID As Integer,
                   userType As String, employeeID As Integer?, companyID As Integer?, companyName As String,
                   mustChangePassword As Boolean, permissions As IEnumerable(Of String))
        Me.UserID = userID
        Me.Username = username
        Me.FullName = fullName
        Me.RoleID = roleID
        Me.UserType = userType
        Me.EmployeeID = employeeID
        Me.CompanyID = companyID
        Me.CompanyName = companyName
        Me.MustChangePassword = mustChangePassword
        _permissions = New HashSet(Of String)(permissions, StringComparer.OrdinalIgnoreCase)
    End Sub

    Public Function Has(permissionCode As String) As Boolean
        Return _permissions.Contains(permissionCode)
    End Function

    ''' <summary>Client-side pre-check. The database enforces the same rule again.</summary>
    Public Sub Require(permissionCode As String)
        If Not Has(permissionCode) Then Throw New BusinessException("You do not have permission for this action.")
    End Sub

    Public Sub RequireCompany()
        If Not CompanyID.HasValue Then Throw New BusinessException("Select a company first.")
    End Sub
End Class
