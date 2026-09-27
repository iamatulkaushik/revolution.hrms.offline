Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>Factory, department, designation.</summary>
Public NotInheritable Class MasterService
    ' ---------- Division ----------
    Public Function Divisions(activeOnly As Boolean) As DataTable
        Return MasterRepo.DivisionList(activeOnly)
    End Function

    Public Function SaveDivision(divisionID As Integer?, name As String) As Integer
        AppSession.Require().Require("FACTORY_EDIT")
        If String.IsNullOrWhiteSpace(name) Then Throw New BusinessException("Division name is required.")
        Return MasterRepo.DivisionSave(divisionID, name.Trim(), Nothing)
    End Function

    Public Sub SetDivisionActive(divisionID As Integer, isActive As Boolean)
        AppSession.Require().Require("FACTORY_EDIT")
        MasterRepo.DivisionSetActive(divisionID, isActive)
    End Sub

    ' ---------- Factory ----------
    Public Function Factories(activeOnly As Boolean) As DataTable
        Return MasterRepo.FactoryList(activeOnly)
    End Function

    Public Function SaveFactory(f As FactoryInfo) As Integer
        AppSession.Require().Require("FACTORY_EDIT")
        If f Is Nothing OrElse String.IsNullOrWhiteSpace(f.Name) Then Throw New BusinessException("Factory name is required.")
        Return MasterRepo.FactorySave(f)
    End Function

    Public Sub SetFactoryActive(factoryID As Integer, isActive As Boolean)
        AppSession.Require().Require("FACTORY_EDIT")
        MasterRepo.FactorySetActive(factoryID, isActive)
    End Sub

    ' ---------- Department ----------
    Public Function Departments(activeOnly As Boolean) As DataTable
        Return MasterRepo.DepartmentList(activeOnly)
    End Function

    Public Function SaveDepartment(deptID As Integer?, name As String) As Integer
        AppSession.Require().Require("EMPLOYEE_EDIT")
        If String.IsNullOrWhiteSpace(name) Then Throw New BusinessException("Department name is required.")
        Return MasterRepo.DepartmentSave(deptID, name.Trim())
    End Function

    Public Sub SetDepartmentActive(deptID As Integer, isActive As Boolean)
        AppSession.Require().Require("EMPLOYEE_EDIT")
        MasterRepo.DepartmentSetActive(deptID, isActive)
    End Sub

    ' ---------- Designation ----------
    Public Function Designations(activeOnly As Boolean) As DataTable
        Return MasterRepo.DesignationList(activeOnly)
    End Function

    Public Function SaveDesignation(desigID As Integer?, name As String) As Integer
        AppSession.Require().Require("EMPLOYEE_EDIT")
        If String.IsNullOrWhiteSpace(name) Then Throw New BusinessException("Designation name is required.")
        Return MasterRepo.DesignationSave(desigID, name.Trim())
    End Function

    Public Sub SetDesignationActive(desigID As Integer, isActive As Boolean)
        AppSession.Require().Require("EMPLOYEE_EDIT")
        MasterRepo.DesignationSetActive(desigID, isActive)
    End Sub

    ' ---------- Shift ----------
    Public Function Shifts(activeOnly As Boolean) As DataTable
        Return MasterRepo.ShiftList(activeOnly)
    End Function

    Public Function SaveShift(sh As ShiftInfo) As Integer
        AppSession.Require().Require("ATT_EDIT")
        If sh Is Nothing OrElse String.IsNullOrWhiteSpace(sh.Name) Then Throw New BusinessException("Shift name is required.")
        If sh.ShiftHours <= 0D OrElse sh.ShiftHours > 24D Then Throw New BusinessException("Shift hours must be between 0 and 24.")
        If sh.EndTime = sh.StartTime Then Throw New BusinessException("Start and end time cannot be the same.")
        Return MasterRepo.ShiftSave(sh)
    End Function

    Public Sub SetShiftActive(shiftID As Integer, isActive As Boolean)
        AppSession.Require().Require("ATT_EDIT")
        MasterRepo.ShiftSetActive(shiftID, isActive)
    End Sub

    ' ---------- Employee lookup ----------
    Public Function EmployeeLookup() As DataTable
        AppSession.Require().Require("EMPLOYEE_VIEW")
        Return MasterRepo.EmployeeLookup()
    End Function
End Class
