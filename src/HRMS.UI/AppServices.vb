Option Strict On
Option Explicit On

Imports System
Imports HRMS.BLL
Imports HRMS.Common

''' <summary>The services every screen uses. Set once at start-up.</summary>
Friend NotInheritable Class AppServices
    Public Shared Property Auth As AuthService
    Public Shared Property Company As CompanyService
    Public Shared Property Masters As MasterService
    Public Shared Property Employees As EmployeeService
    Public Shared Property Reference As ReferenceService
    Public Shared Property Attendance As AttendanceService
    Public Shared Property Config As ConfigService
    Public Shared Property ServerName As String

    ''' <summary>Raised when the active company changes outside the main menu (e.g. a company was just created).</summary>
    Public Shared Event SessionChanged As EventHandler

    Private Sub New()
    End Sub

    Public Shared Sub Initialize(authService As AuthService, crypto As FieldCrypto, serverName As String)
        Auth = authService
        Company = New CompanyService()
        Masters = New MasterService()
        Employees = New EmployeeService(crypto)
        Reference = New ReferenceService()
        Attendance = New AttendanceService()
        Config = New ConfigService()
        AppServices.ServerName = serverName
    End Sub
    Public Shared Sub RaiseSessionChanged()
        RaiseEvent SessionChanged(Nothing, EventArgs.Empty)
    End Sub
End Class
