Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

Public NotInheritable Class CompanyService
    Public Function GetCurrent() As CompanyInfo
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_VIEW")
        s.RequireCompany()
        Return CompanyRepo.GetCurrent()
    End Function

    ''' <summary>Associate only. Returns the new CompanyID; call AuthService.CompleteLogin(newId) to open it.</summary>
    Public Function Create(c As CompanyInfo) As Integer
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_EDIT")
        If s.UserType <> "Associate" Then Throw New BusinessException("Only Associate users can create companies.")
        Validate(c)
        Dim id As Integer = CompanyRepo.Create(c)
        Logger.Info("Company created id=" & id & " by " & s.Username)
        Return id
    End Function

    Public Sub Update(c As CompanyInfo)
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_EDIT")
        s.RequireCompany()
        Validate(c)
        CompanyRepo.Update(c)
    End Sub

    Public Function GetDetail() As CompanyDetail
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_VIEW")
        s.RequireCompany()
        Return CompanyRepo.GetDetail()
    End Function

    ''' <summary>Empty list = valid.</summary>
    Public Shared Function ValidateDetail(d As CompanyDetail) As List(Of String)
        Dim errors As New List(Of String)()
        If Not String.IsNullOrEmpty(d.PIN) AndAlso (d.PIN.Length <> 6 OrElse Not IsDigits(d.PIN)) Then errors.Add("PIN must be 6 digits.")
        If Not String.IsNullOrEmpty(d.Mobile) AndAlso Not Validators.IsValidMobile(d.Mobile) Then errors.Add("Mobile must be a valid 10-digit number.")
        If Not String.IsNullOrEmpty(d.Mobile2) AndAlso Not Validators.IsValidMobile(d.Mobile2) Then errors.Add("Second mobile must be a valid 10-digit number.")
        If Not String.IsNullOrEmpty(d.Email) AndAlso Not Validators.IsValidEmail(d.Email) Then errors.Add("Invalid email address.")
        If Not String.IsNullOrEmpty(d.Email2) AndAlso Not Validators.IsValidEmail(d.Email2) Then errors.Add("Invalid second email address.")
        If Not String.IsNullOrEmpty(d.BankIfsc) AndAlso Not Validators.IsValidIfsc(d.BankIfsc.ToUpperInvariant()) Then errors.Add("Invalid bank IFSC.")
        If d.LabourFrom.HasValue AndAlso d.LabourTo.HasValue AndAlso d.LabourTo.Value < d.LabourFrom.Value Then errors.Add("Labour licence end date is before its start date.")
        If d.PsaraFrom.HasValue AndAlso d.PsaraTo.HasValue AndAlso d.PsaraTo.Value < d.PsaraFrom.Value Then errors.Add("PSARA end date is before its start date.")
        If d.StartDate.HasValue AndAlso d.ShutDate.HasValue AndAlso d.ShutDate.Value < d.StartDate.Value Then errors.Add("Closing date is before the start date.")
        Return errors
    End Function

    Public Sub SaveDetail(d As CompanyDetail)
        Dim s As UserSession = AppSession.Require()
        s.Require("COMPANY_EDIT")
        s.RequireCompany()
        Dim problems As List(Of String) = ValidateDetail(d)
        If problems.Count > 0 Then Throw New BusinessException(String.Join(Environment.NewLine, problems))
        CompanyRepo.SaveDetail(d)
    End Sub

    Friend Shared Function IsDigits(value As String) As Boolean
        For Each ch As Char In value
            If Not Char.IsDigit(ch) Then Return False
        Next
        Return True
    End Function

    Private Shared Sub Validate(c As CompanyInfo)
        If c Is Nothing OrElse String.IsNullOrWhiteSpace(c.Name) Then Throw New BusinessException("Company name is required.")
        If Not String.IsNullOrWhiteSpace(c.PAN) AndAlso Not Validators.IsValidPan(c.PAN.Trim().ToUpperInvariant()) Then
            Throw New BusinessException("Invalid PAN format (e.g. ABCDE1234F).")
        End If
    End Sub
End Class
