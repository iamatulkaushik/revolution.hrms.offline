Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>Employee master rules + field encryption. Encryption happens here, never in the UI or DAL.</summary>
Public NotInheritable Class EmployeeService
    Private Const MIN_WORKING_AGE As Integer = 14
    Private ReadOnly _crypto As FieldCrypto

    Public Sub New(crypto As FieldCrypto)
        If crypto Is Nothing Then Throw New ArgumentNullException("crypto")
        _crypto = crypto
    End Sub

    Public Function Search(text As String, factoryID As Integer?, deptID As Integer?,
                           activeOnly As Boolean, pageNo As Integer, pageSize As Integer) As DataTable
        AppSession.Require().Require("EMPLOYEE_VIEW")
        Return EmployeeRepo.List(text, factoryID, deptID, activeOnly, pageNo, pageSize)
    End Function

    ''' <summary>includeSensitive=True only when the screen really needs the encrypted fields (audited).</summary>
    Public Function Load(employeeID As Integer, includeSensitive As Boolean) As EmployeeRecord
        Return EmployeeRepo.GetByID(employeeID, includeSensitive)
    End Function

    ''' <summary>Decrypts the record's sensitive fields. Values are Nothing when not visible.</summary>
    Public Function ReadSecrets(rec As EmployeeRecord) As EmployeeSecrets
        Dim s As New EmployeeSecrets()
        If rec Is Nothing OrElse Not rec.SensitiveVisible Then Return s
        s.Aadhaar = _crypto.DecryptText(FieldCrypto.PURPOSE_AADHAAR, rec.Cipher.AadhaarEnc)
        s.PAN = _crypto.DecryptText(FieldCrypto.PURPOSE_PAN, rec.Cipher.PANEnc)
        s.BankAcc = _crypto.DecryptText(FieldCrypto.PURPOSE_BANK_ACC, rec.Cipher.BankAccEnc)
        s.UAN = _crypto.DecryptText(FieldCrypto.PURPOSE_UAN, rec.Cipher.UANEnc)
        s.EsiIP = _crypto.DecryptText(FieldCrypto.PURPOSE_ESI_IP, rec.Cipher.EsiIPEnc)
        Return s
    End Function

    ''' <summary>Same values, masked for display (XXXXXXXX1234).</summary>
    Public Shared Function Mask(secrets As EmployeeSecrets) As EmployeeSecrets
        Return New EmployeeSecrets With {
            .Aadhaar = Masker.Mask(secrets.Aadhaar), .PAN = Masker.Mask(secrets.PAN),
            .BankAcc = Masker.Mask(secrets.BankAcc), .UAN = Masker.Mask(secrets.UAN), .EsiIP = Masker.Mask(secrets.EsiIP)}
    End Function

    ''' <summary>Empty list = valid. Empty secret value = keep the stored one.</summary>
    Public Shared Function Validate(rec As EmployeeRecord, secrets As EmployeeSecrets) As List(Of String)
        Dim errors As New List(Of String)()
        If String.IsNullOrWhiteSpace(rec.Name) Then errors.Add("Name is required.")
        If rec.DOJ = Date.MinValue Then errors.Add("Date of joining is required.")
        If rec.DOB.HasValue AndAlso rec.DOB.Value >= rec.DOJ Then errors.Add("Date of birth must be before joining date.")
        If rec.DOB.HasValue AndAlso rec.DOJ.Year - rec.DOB.Value.Year < MIN_WORKING_AGE Then errors.Add("Employee is below the minimum working age.")
        If rec.DOL.HasValue AndAlso rec.DOL.Value < rec.DOJ Then errors.Add("Leaving date cannot be before joining date.")
        If Not String.IsNullOrEmpty(rec.Gender) AndAlso "MFO".IndexOf(rec.Gender) < 0 Then errors.Add("Gender must be M, F or O.")
        If Not String.IsNullOrEmpty(rec.Mobile) AndAlso Not Validators.IsValidMobile(rec.Mobile) Then errors.Add("Mobile must be a valid 10-digit number.")
        If Not String.IsNullOrEmpty(rec.IFSC) AndAlso Not Validators.IsValidIfsc(rec.IFSC.ToUpperInvariant()) Then errors.Add("Invalid IFSC code.")
        If secrets IsNot Nothing Then
            If Not String.IsNullOrWhiteSpace(secrets.Aadhaar) AndAlso Not Validators.IsValidAadhaar(secrets.Aadhaar.Trim()) Then errors.Add("Invalid Aadhaar number.")
            If Not String.IsNullOrWhiteSpace(secrets.PAN) AndAlso Not Validators.IsValidPan(secrets.PAN.Trim().ToUpperInvariant()) Then errors.Add("Invalid PAN.")
            If Not String.IsNullOrWhiteSpace(secrets.BankAcc) AndAlso Not Validators.IsValidBankAccount(secrets.BankAcc.Trim()) Then errors.Add("Bank account must be 9 to 18 digits.")
            If Not String.IsNullOrWhiteSpace(secrets.UAN) AndAlso Not Validators.IsValidUan(secrets.UAN.Trim()) Then errors.Add("UAN must be 12 digits.")
            If Not String.IsNullOrWhiteSpace(secrets.EsiIP) AndAlso Not Validators.IsValidEsiIp(secrets.EsiIP.Trim()) Then errors.Add("ESI IP number must be 10 or 17 digits.")
        End If
        Return errors
    End Function

    ''' <summary>Creates (EmployeeID = 0) or updates. Returns the EmployeeID.</summary>
    Public Function Save(rec As EmployeeRecord, secrets As EmployeeSecrets) As Integer
        Dim s As UserSession = AppSession.Require()
        s.Require("EMPLOYEE_EDIT")
        s.RequireCompany()
        Dim problems As List(Of String) = Validate(rec, secrets)
        If problems.Count > 0 Then Throw New BusinessException(String.Join(Environment.NewLine, problems))

        Dim hasSecrets As Boolean = secrets IsNot Nothing AndAlso
            (Not String.IsNullOrWhiteSpace(secrets.Aadhaar) OrElse Not String.IsNullOrWhiteSpace(secrets.PAN) OrElse
             Not String.IsNullOrWhiteSpace(secrets.BankAcc) OrElse Not String.IsNullOrWhiteSpace(secrets.UAN) OrElse
             Not String.IsNullOrWhiteSpace(secrets.EsiIP))
        If hasSecrets Then s.Require("EMPLOYEE_SENSITIVE")

        rec.Cipher = New EmployeeCipher()
        If hasSecrets Then
            rec.Cipher.AadhaarEnc = _crypto.EncryptText(FieldCrypto.PURPOSE_AADHAAR, secrets.Aadhaar)
            rec.Cipher.PANEnc = _crypto.EncryptText(FieldCrypto.PURPOSE_PAN, Upper(secrets.PAN))
            rec.Cipher.BankAccEnc = _crypto.EncryptText(FieldCrypto.PURPOSE_BANK_ACC, secrets.BankAcc)
            rec.Cipher.UANEnc = _crypto.EncryptText(FieldCrypto.PURPOSE_UAN, secrets.UAN)
            rec.Cipher.EsiIPEnc = _crypto.EncryptText(FieldCrypto.PURPOSE_ESI_IP, secrets.EsiIP)
        End If

        If rec.EmployeeID = 0 Then
            Return EmployeeRepo.Create(rec)
        End If
        EmployeeRepo.Update(rec)
        Return rec.EmployeeID
    End Function

    Public Sub ExitEmployee(employeeID As Integer, dol As Date)
        AppSession.Require().Require("EMPLOYEE_EDIT")
        EmployeeRepo.ExitEmployee(employeeID, dol)
    End Sub

    Public Sub Reactivate(employeeID As Integer)
        AppSession.Require().Require("EMPLOYEE_EDIT")
        EmployeeRepo.Reactivate(employeeID)
    End Sub

    ' ---------- Detail ----------
    Public Function LoadDetail(employeeID As Integer) As EmployeeDetail
        AppSession.Require().Require("EMPLOYEE_VIEW")
        Return EmployeeRepo.GetDetail(employeeID)
    End Function

    ''' <summary>Empty list = valid.</summary>
    Public Shared Function ValidateDetail(d As EmployeeDetail) As List(Of String)
        Dim errors As New List(Of String)()
        If Not String.IsNullOrEmpty(d.MaritalStatus) AndAlso "UMWD".IndexOf(d.MaritalStatus) < 0 Then errors.Add("Invalid marital status.")
        If Not String.IsNullOrEmpty(d.Mobile2) AndAlso Not Validators.IsValidMobile(d.Mobile2) Then errors.Add("Second mobile must be a valid 10-digit number.")
        If Not String.IsNullOrEmpty(d.Email) AndAlso Not Validators.IsValidEmail(d.Email) Then errors.Add("Invalid email address.")
        If Not String.IsNullOrEmpty(d.Email2) AndAlso Not Validators.IsValidEmail(d.Email2) Then errors.Add("Invalid second email address.")
        If Not String.IsNullOrEmpty(d.PresentPIN) AndAlso (d.PresentPIN.Length <> 6 OrElse Not CompanyService.IsDigits(d.PresentPIN)) Then errors.Add("Present address PIN must be 6 digits.")
        If Not String.IsNullOrEmpty(d.PermPIN) AndAlso (d.PermPIN.Length <> 6 OrElse Not CompanyService.IsDigits(d.PermPIN)) Then errors.Add("Permanent address PIN must be 6 digits.")
        If d.EduPercent.HasValue AndAlso (d.EduPercent.Value < 0D OrElse d.EduPercent.Value > 100D) Then errors.Add("Percentage must be between 0 and 100.")
        If d.PaymentType < 1 OrElse d.PaymentType > 3 Then errors.Add("Invalid payment type.")
        If d.EmploymentType < 1 OrElse d.EmploymentType > 4 Then errors.Add("Invalid employment type.")
        Return errors
    End Function

    Public Sub SaveDetail(employeeID As Integer, d As EmployeeDetail)
        AppSession.Require().Require("EMPLOYEE_EDIT")
        Dim problems As List(Of String) = ValidateDetail(d)
        If problems.Count > 0 Then Throw New BusinessException(String.Join(Environment.NewLine, problems))
        EmployeeRepo.SaveDetail(employeeID, d)
    End Sub

    ' ---------- Nominees ----------
    Public Function Nominees(employeeID As Integer) As DataTable
        AppSession.Require().Require("EMPLOYEE_VIEW")
        Return EmployeeRepo.NomineeList(employeeID)
    End Function

    Public Function SaveNominee(n As NomineeInfo) As Integer
        AppSession.Require().Require("EMPLOYEE_EDIT")
        If n Is Nothing OrElse String.IsNullOrWhiteSpace(n.Name) Then Throw New BusinessException("Nominee name is required.")
        If n.SharePct <= 0D OrElse n.SharePct > 100D Then Throw New BusinessException("Nominee share must be between 1 and 100.")
        Return EmployeeRepo.NomineeSave(n)
    End Function

    Public Sub DeleteNominee(nomineeID As Integer)
        AppSession.Require().Require("EMPLOYEE_EDIT")
        EmployeeRepo.NomineeDelete(nomineeID)
    End Sub

    Private Shared Function Upper(value As String) As String
        Return If(value, String.Empty).Trim().ToUpperInvariant()
    End Function
End Class
