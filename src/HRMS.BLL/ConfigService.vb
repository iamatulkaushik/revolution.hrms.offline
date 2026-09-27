Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL

''' <summary>Financial year setup (cfg.FinancialYear). Overlap and current-FY rules enforced here and in the proc.</summary>
Public NotInheritable Class ConfigService
    Public Function FinancialYears(Optional activeOnly As Boolean = True) As DataTable
        AppSession.Require()
        Return ConfigRepo.FinancialYearList(activeOnly)
    End Function

    Public Function SaveFinancialYear(fyID As Integer?, fyName As String, fromDate As Date, toDate As Date) As Integer
        AppSession.Require().Require("COMPANY_EDIT")
        If String.IsNullOrWhiteSpace(fyName) Then Throw New BusinessException("Financial year name is required.")
        If toDate <= fromDate Then Throw New BusinessException("End date must be after the start date.")
        Return ConfigRepo.FinancialYearSave(fyID, fyName.Trim(), fromDate, toDate)
    End Function

    Public Sub SetCurrentFinancialYear(fyID As Integer)
        AppSession.Require().Require("COMPANY_EDIT")
        ConfigRepo.FinancialYearSetCurrent(fyID)
    End Sub

    ' ---------- Holiday ----------
    Public Function Holidays(Optional [year] As Integer? = Nothing) As DataTable
        AppSession.Require()
        Return ConfigRepo.HolidayList([year])
    End Function

    Public Function SaveHoliday(holidayID As Integer?, holidayDate As Date, name As String) As Integer
        AppSession.Require().Require("ATT_EDIT")
        If String.IsNullOrWhiteSpace(name) Then Throw New BusinessException("Holiday name is required.")
        Return ConfigRepo.HolidaySave(holidayID, holidayDate, name.Trim())
    End Function

    Public Sub SetHolidayActive(holidayID As Integer, isActive As Boolean)
        AppSession.Require().Require("ATT_EDIT")
        ConfigRepo.HolidaySetActive(holidayID, isActive)
    End Sub
End Class
