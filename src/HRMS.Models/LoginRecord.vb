Option Strict On
Option Explicit On

Imports System

''' <summary>Credential row from sec.usp_User_GetForLogin.</summary>
Public NotInheritable Class LoginRecord
    Public Property UserID As Integer
    Public Property Username As String
    Public Property FullName As String
    Public Property PasswordHash As Byte()
    Public Property Salt As Byte()
    Public Property Iterations As Integer
    Public Property RoleID As Integer
    Public Property UserType As String
    Public Property HomeCompanyID As Integer?
    Public Property EmployeeID As Integer?
    Public Property FailedCount As Integer
    Public Property LockedUntil As Date?
    Public Property ServerNow As Date
    Public Property MustChangePwd As Boolean
    Public Property IsActive As Boolean
End Class
