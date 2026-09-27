Option Strict On
Option Explicit On

Imports System
Imports HRMS.Common
Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Financial year and holiday calendar procs (cfg schema, scoped to session company by RLS).</summary>
Public NotInheritable Class ConfigRepo
    Private Sub New()
    End Sub

    ' ---------- Financial Year ----------
    Public Shared Function FinancialYearList(Optional activeOnly As Boolean = True) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "cfg.usp_FinancialYear_List", Db.P("@ActiveOnly", activeOnly))
        End Using
    End Function

    Public Shared Function FinancialYearSave(fyID As Integer?, fyName As String, fromDate As Date, toDate As Date) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "cfg.usp_FinancialYear_Save",
                Db.P("@FYID", fyID), Db.P("@FYName", fyName), Db.P("@FromDate", fromDate), Db.P("@ToDate", toDate)))
        End Using
    End Function

    Public Shared Sub FinancialYearSetCurrent(fyID As Integer)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "cfg.usp_FinancialYear_SetCurrent", Db.P("@FYID", fyID))
        End Using
    End Sub

    ' ---------- Holiday ----------
    Public Shared Function HolidayList(Optional [year] As Integer? = Nothing) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "cfg.usp_Holiday_List", Db.P("@Year", [year]))
        End Using
    End Function

    Public Shared Function HolidaySave(holidayID As Integer?, holidayDate As Date, name As String) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "cfg.usp_Holiday_Save",
                Db.P("@HolidayID", holidayID), Db.P("@HolidayDate", holidayDate), Db.P("@Name", name)))
        End Using
    End Function

    Public Shared Sub HolidaySetActive(holidayID As Integer, isActive As Boolean)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "cfg.usp_Holiday_SetActive", Db.P("@HolidayID", holidayID), Db.P("@IsActive", isActive))
        End Using
    End Sub
End Class
