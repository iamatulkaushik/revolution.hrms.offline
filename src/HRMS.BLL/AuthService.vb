Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>Login flow: Login (password check) -> GetCompanies -> CompleteLogin (session).
''' Passwords are hashed here (PBKDF2); the database only stores hash + salt.</summary>
Public NotInheritable Class AuthService
    Public Const MAX_FAILS As Integer = 5
    Public Const LOCK_MINUTES As Integer = 15
    Private Const BAD_LOGIN_MESSAGE As String = "Invalid username or password."

    Private Shared ReadOnly DUMMY_SALT As Byte() = New Byte(31) {}

    Private _pending As LoginRecord

    ' ---------- first run ----------
    Public Function IsBootstrapNeeded() As Boolean
        Return AuthRepo.BootstrapNeeded()
    End Function

    Public Sub Bootstrap(username As String, fullName As String, password As String)
        Dim uname As String = CleanUsername(username)
        Dim problem As String = PasswordPolicy.Validate(password, uname)
        If problem IsNot Nothing Then Throw New BusinessException(problem)
        Dim salt As Byte() = PasswordHasher.NewSalt()
        Dim hash As Byte() = PasswordHasher.Hash(password, salt, PasswordHasher.DEFAULT_ITERATIONS)
        AuthRepo.BootstrapAdmin(uname, RequireText(fullName, "Full name"), hash, salt, PasswordHasher.DEFAULT_ITERATIONS)
        Logger.Info("Bootstrap admin created: " & uname)
    End Sub

    ' ---------- login ----------
    Public Function Login(username As String, password As String) As LoginOutcome
        _pending = Nothing
        Dim rec As LoginRecord = Nothing
        Dim outcome As LoginOutcome = Authenticate(username, password, rec)
        If outcome.IsSuccess Then _pending = rec
        Return outcome
    End Function

    ''' <summary>Re-checks the logged-in user's password (unlock screen). Counts failures and locks like a login.</summary>
    Public Function VerifyUnlock(password As String) As LoginOutcome
        Dim s As UserSession = AppSession.Require()
        Dim rec As LoginRecord = Nothing
        Return Authenticate(s.Username, password, rec)
    End Function

    Private Function Authenticate(username As String, password As String, ByRef rec As LoginRecord) As LoginOutcome
        Dim uname As String = If(username, String.Empty).Trim()
        If uname.Length = 0 OrElse String.IsNullOrEmpty(password) Then Return Fail(LoginStatus.BadCredentials, BAD_LOGIN_MESSAGE)

        rec = AuthRepo.GetForLogin(uname)
        If rec Is Nothing Then
            PasswordHasher.Hash(password, DUMMY_SALT, PasswordHasher.DEFAULT_ITERATIONS) ' equalise timing
            Logger.Warn("Login failed (unknown user): " & uname)
            Return Fail(LoginStatus.BadCredentials, BAD_LOGIN_MESSAGE)
        End If
        If Not rec.IsActive Then
            Logger.Warn("Login refused (inactive): " & uname)
            Return Fail(LoginStatus.Inactive, "This account is disabled.")
        End If
        If rec.LockedUntil.HasValue AndAlso rec.LockedUntil.Value > rec.ServerNow Then
            Logger.Warn("Login refused (locked): " & uname)
            Return Fail(LoginStatus.Locked, "Account temporarily locked. Try again later.")
        End If

        Dim ok As Boolean = PasswordHasher.Verify(password, rec.Salt, rec.Iterations, rec.PasswordHash)
        AuthRepo.RecordLoginResult(rec.UserID, ok, MAX_FAILS, LOCK_MINUTES)
        If Not ok Then
            Logger.Warn("Login failed (bad password): " & uname)
            ' the attempt that reaches the limit locks the account: tell the user now
            If rec.FailedCount + 1 >= MAX_FAILS Then Return Fail(LoginStatus.Locked, "Account temporarily locked. Try again later.")
            Return Fail(LoginStatus.BadCredentials, BAD_LOGIN_MESSAGE)
        End If

        Return New LoginOutcome With {
            .Status = LoginStatus.Success, .Message = "OK", .UserID = rec.UserID,
            .UserType = rec.UserType, .MustChangePassword = rec.MustChangePwd}
    End Function

    ''' <summary>Companies the logged-in user may open (ID, Name).</summary>
    Public Function GetCompanies() As DataTable
        Return AuthRepo.GetCompanies(RequirePending().UserID)
    End Function

    ''' <summary>Creates the session. companyID may be Nothing only for an Associate (e.g. to create the first company).</summary>
    Public Sub CompleteLogin(companyID As Integer?)
        Dim rec As LoginRecord = RequirePending()
        Dim companyName As String = Nothing
        If companyID.HasValue Then
            For Each row As DataRow In AuthRepo.GetCompanies(rec.UserID).Rows
                If Convert.ToInt32(row("CompanyID")) = companyID.Value Then companyName = Convert.ToString(row("Name"))
            Next
            If companyName Is Nothing Then Throw New BusinessException("This company is not available for your account.")
        ElseIf rec.UserType <> "Associate" Then
            Throw New BusinessException("Company required.")
        End If

        Dim previous As UserSession = AppSession.Current
        AppSession.Current = New UserSession(rec.UserID, rec.Username, rec.FullName, rec.RoleID, rec.UserType,
                                             rec.EmployeeID, companyID, companyName, rec.MustChangePwd,
                                             AuthRepo.GetPermissions(rec.UserID))
        Try
            Db.ValidateSession()
        Catch ex As BusinessException
            AppSession.Current = previous   ' keep the old session when switching company fails
            Throw
        End Try
        Logger.Info("Session started: user=" & rec.Username & " company=" & If(companyName, "(none)"))
    End Sub

    Public Sub Logout()
        If AppSession.IsLoggedIn Then Logger.Info("Logout: " & AppSession.Current.Username)
        _pending = Nothing
        AppSession.Clear()
    End Sub

    ' ---------- password ----------
    Public Sub ChangePassword(currentPassword As String, newPassword As String)
        Dim s As UserSession = AppSession.Require()
        Dim rec As LoginRecord = AuthRepo.GetForLogin(s.Username)
        If rec Is Nothing OrElse Not PasswordHasher.Verify(currentPassword, rec.Salt, rec.Iterations, rec.PasswordHash) Then
            Throw New BusinessException("Current password is incorrect.")
        End If
        Dim problem As String = PasswordPolicy.Validate(newPassword, s.Username)
        If problem IsNot Nothing Then Throw New BusinessException(problem)
        If String.Equals(currentPassword, newPassword, StringComparison.Ordinal) Then
            Throw New BusinessException("New password must be different.")
        End If
        Dim salt As Byte() = PasswordHasher.NewSalt()
        Dim hash As Byte() = PasswordHasher.Hash(newPassword, salt, PasswordHasher.DEFAULT_ITERATIONS)
        AuthRepo.ChangePassword(s.UserID, hash, salt, PasswordHasher.DEFAULT_ITERATIONS)
        s.MustChangePassword = False
        Logger.Info("Password changed: " & s.Username)
    End Sub

    ' ---------- user administration ----------
    Public Function CreateUser(username As String, fullName As String, initialPassword As String,
                               roleID As Integer, homeCompanyID As Integer?, employeeID As Integer?) As Integer
        Dim s As UserSession = AppSession.Require()
        s.Require("USER_ADMIN")
        Dim uname As String = CleanUsername(username)
        Dim problem As String = PasswordPolicy.Validate(initialPassword, uname)
        If problem IsNot Nothing Then Throw New BusinessException(problem)
        Dim salt As Byte() = PasswordHasher.NewSalt()
        Dim hash As Byte() = PasswordHasher.Hash(initialPassword, salt, PasswordHasher.DEFAULT_ITERATIONS)
        Dim id As Integer = AuthRepo.CreateUser(uname, RequireText(fullName, "Full name"), hash, salt,
                                                PasswordHasher.DEFAULT_ITERATIONS, roleID, homeCompanyID, employeeID, s.UserID)
        Logger.Info("User created: " & uname & " by " & s.Username)
        Return id
    End Function

    Public Function ListUsers(companyID As Integer?) As DataTable
        AppSession.Require().Require("USER_ADMIN")
        Return AuthRepo.ListUsers(companyID)
    End Function

    Public Function ListRoles() As DataTable
        AppSession.Require().Require("USER_ADMIN")
        Return AuthRepo.ListRoles()
    End Function

    Public Sub UnlockUser(userID As Integer)
        Dim s As UserSession = AppSession.Require()
        s.Require("USER_ADMIN")
        AuthRepo.Unlock(userID, s.UserID)
    End Sub

    Public Sub SetUserActive(userID As Integer, isActive As Boolean)
        Dim s As UserSession = AppSession.Require()
        s.Require("USER_ADMIN")
        If userID = s.UserID AndAlso Not isActive Then Throw New BusinessException("You cannot disable your own account.")
        AuthRepo.SetActive(userID, isActive, s.UserID)
    End Sub

    ' ---------- helpers ----------
    Private Function RequirePending() As LoginRecord
        If _pending Is Nothing Then Throw New InvalidOperationException("Login first.")
        Return _pending
    End Function

    Private Shared Function Fail(status As LoginStatus, message As String) As LoginOutcome
        Return New LoginOutcome With {.Status = status, .Message = message}
    End Function

    Private Shared Function CleanUsername(username As String) As String
        Dim u As String = If(username, String.Empty).Trim()
        If u.Length < 3 OrElse u.Length > 50 Then Throw New BusinessException("Username must be 3 to 50 characters.")
        Return u
    End Function

    Private Shared Function RequireText(value As String, label As String) As String
        Dim v As String = If(value, String.Empty).Trim()
        If v.Length = 0 Then Throw New BusinessException(label & " is required.")
        Return v
    End Function
End Class
