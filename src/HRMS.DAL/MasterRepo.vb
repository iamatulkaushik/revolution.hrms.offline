Option Strict On
Option Explicit On

Imports System
Imports HRMS.Common
Imports HRMS.Models
Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Factory, department, designation procs (all scoped to the session company by RLS).</summary>
Public NotInheritable Class MasterRepo
    Private Sub New()
    End Sub

    ' ---------- Division ----------
    Public Shared Function DivisionList(activeOnly As Boolean) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Division_List", Db.P("@ActiveOnly", activeOnly))
        End Using
    End Function

    Public Shared Function DivisionSave(divisionID As Integer?, name As String, address As String) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_Division_Save", Db.P("@DivisionID", divisionID), Db.P("@Name", name), Db.P("@Address", address)))
        End Using
    End Function

    Public Shared Sub DivisionSetActive(divisionID As Integer, isActive As Boolean)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Division_SetActive", Db.P("@DivisionID", divisionID), Db.P("@IsActive", isActive))
        End Using
    End Sub

    ' ---------- Factory ----------
    Public Shared Function FactoryList(activeOnly As Boolean) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Factory_List", Db.P("@ActiveOnly", activeOnly))
        End Using
    End Function

    Public Shared Function FactorySave(f As FactoryInfo) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_Factory_Save", Db.P("@FactoryID", f.FactoryID),
                Db.P("@Name", f.Name), Db.P("@LicenseNo", f.LicenseNo), Db.P("@Address", f.Address), Db.P("@Zone", f.Zone),
                Db.P("@DivisionID", f.DivisionID)))
        End Using
    End Function

    Public Shared Sub FactorySetActive(factoryID As Integer, isActive As Boolean)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Factory_SetActive", Db.P("@FactoryID", factoryID), Db.P("@IsActive", isActive))
        End Using
    End Sub

    ' ---------- Department ----------
    Public Shared Function DepartmentList(activeOnly As Boolean) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Department_List", Db.P("@ActiveOnly", activeOnly))
        End Using
    End Function

    Public Shared Function DepartmentSave(deptID As Integer?, name As String) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_Department_Save", Db.P("@DeptID", deptID), Db.P("@Name", name)))
        End Using
    End Function

    Public Shared Sub DepartmentSetActive(deptID As Integer, isActive As Boolean)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Department_SetActive", Db.P("@DeptID", deptID), Db.P("@IsActive", isActive))
        End Using
    End Sub

    ' ---------- Designation ----------
    Public Shared Function DesignationList(activeOnly As Boolean) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Designation_List", Db.P("@ActiveOnly", activeOnly))
        End Using
    End Function

    Public Shared Function DesignationSave(desigID As Integer?, name As String) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_Designation_Save", Db.P("@DesigID", desigID), Db.P("@Name", name)))
        End Using
    End Function

    Public Shared Sub DesignationSetActive(desigID As Integer, isActive As Boolean)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Designation_SetActive", Db.P("@DesigID", desigID), Db.P("@IsActive", isActive))
        End Using
    End Sub
    ' ---------- Minimum Wage ----------
    Public Shared Function MinWageList(Optional zone As String = Nothing, Optional skillCategory As String = Nothing, Optional asOfDate As Date? = Nothing) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_MinWage_List", Db.P("@Zone", zone), Db.P("@SkillCategory", skillCategory), Db.P("@AsOfDate", asOfDate))
        End Using
    End Function

    Public Shared Function MinWageSave(zone As String, skillCategory As String, dailyRate As Decimal, monthlyRate As Decimal, fromDate As Date) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_MinWage_Save",
                Db.P("@Zone", zone), Db.P("@SkillCategory", skillCategory),
                Db.P("@DailyRate", dailyRate), Db.P("@MonthlyRate", monthlyRate), Db.P("@FromDate", fromDate)))
        End Using
    End Function
    ' ---------- Shift ----------
    Public Shared Function ShiftList(activeOnly As Boolean) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "att.usp_Shift_List", Db.P("@ActiveOnly", activeOnly))
        End Using
    End Function

    Public Shared Function ShiftSave(sh As ShiftInfo) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "att.usp_Shift_Save", Db.P("@ShiftID", sh.ShiftID),
                Db.P("@Name", sh.Name), Db.P("@StartTime", sh.StartTime), Db.P("@EndTime", sh.EndTime),
                Db.P("@ShiftHours", sh.ShiftHours)))
        End Using
    End Function

    Public Shared Sub ShiftSetActive(shiftID As Integer, isActive As Boolean)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "att.usp_Shift_SetActive", Db.P("@ShiftID", shiftID), Db.P("@IsActive", isActive))
        End Using
    End Sub

    ' ---------- Employee lookup (read-only picker for other screens; full CRUD lives in EmployeeRepo) ----------
    Public Shared Function EmployeeLookup() As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Employee_List", Db.P("@Search", Nothing), Db.P("@FactoryID", Nothing),
                Db.P("@DeptID", Nothing), Db.P("@ActiveOnly", True), Db.P("@PageNo", 1), Db.P("@PageSize", 2000))
        End Using
    End Function
End Class
