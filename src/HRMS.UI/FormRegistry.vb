Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms

''' <summary>Maps a menu key (see MenuCatalog) to the form that implements it.
''' Later phases call Register at start-up; keys without a form show a "planned" message.</summary>
Friend NotInheritable Class FormRegistry
    Private Shared ReadOnly _factories As New Dictionary(Of String, Func(Of Form))(StringComparer.OrdinalIgnoreCase)

    Private Sub New()
    End Sub

    ''' <summary>Screens available so far. Add a line per new screen.</summary>
    Public Shared Sub RegisterDefaults()
        Register("mst.company", Function() New frmCompany())
        Register("mst.factory", Function() New frmFactory())
        Register("mst.dept", Function() New frmLookupMaster("Departments", "Department", "DeptID",
            Function(active) AppServices.Masters.Departments(active),
            Function(id, name) AppServices.Masters.SaveDepartment(id, name),
            Sub(id, active) AppServices.Masters.SetDepartmentActive(id, active), "EMPLOYEE_EDIT"))
        Register("mst.desig", Function() New frmLookupMaster("Designations", "Designation", "DesigID",
            Function(active) AppServices.Masters.Designations(active),
            Function(id, name) AppServices.Masters.SaveDesignation(id, name),
            Sub(id, active) AppServices.Masters.SetDesignationActive(id, active), "EMPLOYEE_EDIT"))
        Register("mst.division", Function() New frmLookupMaster("Divisions", "Division", "DivisionID",
            Function(active) AppServices.Masters.Divisions(active),
            Function(id, name) AppServices.Masters.SaveDivision(id, name),
            Sub(id, active) AppServices.Masters.SetDivisionActive(id, active), "FACTORY_EDIT"))
        Register("mst.employee", Function() New frmEmployeeList())
        Register("mst.fy", Function() New frmFinancialYear())
        Register("mst.holiday", Function() New frmHolidayCalendar())
        Register("att.monthly", Function() New frmMonthlyAttendance())
    End Sub

    Public Shared Sub Register(key As String, factory As Func(Of Form))
        _factories(key) = factory
    End Sub

    Public Shared Function TryCreate(key As String, ByRef form As Form) As Boolean
        Dim factory As Func(Of Form) = Nothing
        If _factories.TryGetValue(key, factory) Then
            form = factory()
            Return True
        End If
        form = Nothing
        Return False
    End Function
End Class
