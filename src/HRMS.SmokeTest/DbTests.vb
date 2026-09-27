Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports HRMS.BLL
Imports HRMS.Common
Imports HRMS.DAL
Imports HRMS.Models

''' <summary>End-to-end checks of scripts 0001-0012 through DAL + BLL. Creates "Smoke Test Co" data
''' (see db/tools/cleanup_smoke.sql). Safe to re-run.</summary>
Public NotInheritable Class DbTests
    Private Const CO1 As String = "Smoke Test Co"
    Private Const CO2 As String = "Smoke Test Co 2"
    Private Const AADHAAR As String = "234123412346"

    Private Sub New()
    End Sub

    Public Shared Sub Run(connectionString As String, crypto As FieldCrypto, adminPassword As String)
        TestRunner.Section("Database tests")
        Db.Configure(connectionString)
        Dim auth As New AuthService()

        TestRunner.Check("db: connect + bootstrap check", Function()
                                                              auth.IsBootstrapNeeded()
                                                              Return True
                                                          End Function)
        If auth.IsBootstrapNeeded() Then
            auth.Bootstrap("admin", "Administrator", adminPassword)
            TestRunner.Info("bootstrap admin created: username 'admin'")
        End If

        TestRunner.Check("auth: wrong password rejected", Function() auth.Login("admin", adminPassword & "x").Status = LoginStatus.BadCredentials)
        TestRunner.Check("auth: unknown user rejected", Function() auth.Login("no_such_user", "whatever1").Status = LoginStatus.BadCredentials)
        Dim outcome As LoginOutcome = auth.Login("admin", adminPassword)
        TestRunner.Check("auth: correct login", Function() outcome.IsSuccess)
        If Not outcome.IsSuccess Then
            TestRunner.Info("login failed (" & outcome.Message & "); skipping remaining DB tests")
            Return
        End If

        ' Associate with no company yet
        auth.CompleteLogin(Nothing)
        TestRunner.Check("session: associate without company", Function() AppSession.Current.CompanyID Is Nothing)
        Dim company As New CompanyService()
        Dim id1 As Integer = FindCompany(auth, CO1)
        If id1 = 0 Then id1 = company.Create(New CompanyInfo With {.Name = CO1, .PAN = "ABCDE1234F", .Address = "Rohtak"})
        Dim id2 As Integer = FindCompany(auth, CO2)
        If id2 = 0 Then id2 = company.Create(New CompanyInfo With {.Name = CO2})
        TestRunner.Check("company: two companies exist", Function() id1 > 0 AndAlso id2 > 0 AndAlso id1 <> id2)
        TestRunner.Expect(Of BusinessException)("company: bad PAN rejected", Sub() company.Create(New CompanyInfo With {.Name = "x", .PAN = "BAD"}))

        auth.CompleteLogin(id1)
        TestRunner.Check("session: company 1 selected", Function() AppSession.Current.CompanyID.HasValue AndAlso AppSession.Current.CompanyID.Value = id1)
        TestRunner.Check("company: read back", Function() company.GetCurrent().Name = CO1)

        Dim masters As New MasterService()
        Dim factoryID As Integer = EnsureFactory(masters, "Smoke Factory")
        Dim deptID As Integer = EnsureLookup(masters.Departments(False), "DeptID", "Smoke Dept", Function(n) masters.SaveDepartment(Nothing, n))
        Dim desigID As Integer = EnsureLookup(masters.Designations(False), "DesigID", "Smoke Desig", Function(n) masters.SaveDesignation(Nothing, n))
        TestRunner.Check("masters: factory/dept/desig ready", Function() factoryID > 0 AndAlso deptID > 0 AndAlso desigID > 0)
        TestRunner.Expect(Of BusinessException)("masters: duplicate department rejected", Sub() masters.SaveDepartment(Nothing, "Smoke Dept"))

        Dim emps As New EmployeeService(crypto)
        Dim rec As New EmployeeRecord With {
            .Name = "Smoke Employee " & Date.Now.ToString("HHmmss"), .FatherName = "Test Father", .Gender = "M",
            .DOB = New Date(1990, 5, 12), .DOJ = New Date(2024, 4, 1), .FactoryID = factoryID, .DeptID = deptID,
            .DesigID = desigID, .SkillCategory = "Skilled", .Mobile = "9876543210", .IFSC = "SBIN0001234", .Address = "Rohtak"}
        Dim secrets As New EmployeeSecrets With {.Aadhaar = AADHAAR, .PAN = "abcde1234f", .BankAcc = "123456789012", .UAN = "123456789012", .EsiIP = "1234567890"}

        TestRunner.Expect(Of BusinessException)("employee: invalid Aadhaar rejected", Sub() emps.Save(New EmployeeRecord With {.Name = "x", .DOJ = New Date(2024, 1, 1)}, New EmployeeSecrets With {.Aadhaar = "234123412345"}))
        Dim empID As Integer = emps.Save(rec, secrets)
        TestRunner.Check("employee: created with auto code", Function() empID > 0 AndAlso rec.EmpCode.StartsWith("E"))
        TestRunner.Info("employee " & rec.EmpCode & " id=" & empID)

        Dim plain As EmployeeRecord = emps.Load(empID, False)
        TestRunner.Check("employee: sensitive hidden by default", Function() Not plain.SensitiveVisible AndAlso plain.Cipher.AadhaarEnc Is Nothing)
        TestRunner.Check("employee: presence flags without ciphertext", Function() plain.HasAadhaar AndAlso plain.HasPAN AndAlso plain.HasBankAcc AndAlso plain.HasUAN AndAlso plain.HasEsiIP AndAlso Not plain.SensitiveVisible)
        Dim full As EmployeeRecord = emps.Load(empID, True)
        Dim back As EmployeeSecrets = emps.ReadSecrets(full)
        TestRunner.Check("employee: sensitive round trip (DB stores ciphertext only)", Function() full.SensitiveVisible AndAlso back.Aadhaar = AADHAAR AndAlso back.PAN = "ABCDE1234F" AndAlso back.BankAcc = "123456789012")
        TestRunner.Check("employee: masked view", Function() EmployeeService.Mask(back).Aadhaar = "XXXXXXXX2346")

        full.Address = "Updated address"
        emps.Save(full, New EmployeeSecrets())
        TestRunner.Check("employee: update keeps sensitive when blank", Function() emps.ReadSecrets(emps.Load(empID, True)).Aadhaar = AADHAAR AndAlso emps.Load(empID, False).Address = "Updated address")
        TestRunner.Check("employee: search by name", Function() emps.Search(rec.Name, Nothing, Nothing, True, 1, 50).Rows.Count = 1)

        Dim n1 As Integer = emps.SaveNominee(New NomineeInfo With {.EmployeeID = empID, .Name = "Nominee A", .Relation = "Spouse", .SharePct = 60D})
        TestRunner.Expect(Of BusinessException)("nominee: total over 100% rejected", Sub() emps.SaveNominee(New NomineeInfo With {.EmployeeID = empID, .Name = "Nominee B", .SharePct = 50D}))
        emps.SaveNominee(New NomineeInfo With {.EmployeeID = empID, .Name = "Nominee B", .Relation = "Child", .SharePct = 40D})
        TestRunner.Check("nominee: two saved", Function() emps.Nominees(empID).Rows.Count >= 2 AndAlso n1 > 0)

        DetailTests(company, masters, emps, empID, factoryID)
        AttendanceDbTests(empID, rec.EmpCode)

        ' tenant isolation (RLS)
        auth.CompleteLogin(id2)
        TestRunner.Check("rls: company 2 sees no company-1 employees", Function() emps.Search(rec.Name, Nothing, Nothing, False, 1, 50).Rows.Count = 0)
        TestRunner.Expect(Of BusinessException)("rls: company 2 cannot read company-1 employee", Sub() emps.Load(empID, False))
        TestRunner.Check("rls: company 2 sees no company-1 departments", Function() masters.Departments(False).Rows.Count = 0)
        auth.CompleteLogin(id1)

        emps.ExitEmployee(empID, New Date(2025, 3, 31))
        TestRunner.Check("employee: exit sets DOL and deactivates", Function() Not emps.Load(empID, False).IsActive AndAlso emps.Load(empID, False).DOL.HasValue)
        emps.Reactivate(empID)
        TestRunner.Check("employee: reactivate", Function() emps.Load(empID, False).IsActive)

        AuthTests(auth, adminPassword, id1, id2, crypto)
        auth.Logout()
        TestRunner.Expect(Of BusinessException)("session: data access after logout refused", Sub() emps.Search(Nothing, Nothing, Nothing, True, 1, 10))
    End Sub

    ''' <summary>Users, lockout, forced password change, permissions. Uses a fresh "smoke_hr_*" user each run.</summary>
    Private Shared Sub AuthTests(adminAuth As AuthService, adminPassword As String, companyID As Integer, otherCompanyID As Integer, crypto As FieldCrypto)
        TestRunner.Section("Users, lockout, permissions")
        Const HR_PWD As String = "Smoke#Pass1"
        Const HR_NEW As String = "Smoke#Pass2"
        Dim hrName As String = "smoke_hr_" & Date.Now.ToString("HHmmss")

        Dim hrRole As Integer = FindRole(adminAuth, "Company HR")
        TestRunner.Check("roles: Company HR exists", Function() hrRole > 0)
        TestRunner.Expect(Of BusinessException)("user: weak password refused", Sub() adminAuth.CreateUser(hrName, "Smoke HR", "abc", hrRole, companyID, Nothing))
        Dim hrID As Integer = adminAuth.CreateUser(hrName, "Smoke HR", HR_PWD, hrRole, companyID, Nothing)
        TestRunner.Check("user: created", Function() hrID > 0)
        TestRunner.Expect(Of BusinessException)("user: duplicate username refused", Sub() adminAuth.CreateUser(hrName, "Again", HR_PWD, hrRole, companyID, Nothing))
        adminAuth.Logout()

        ' lockout: 4 bad attempts, the 5th locks, then even the right password is refused
        Dim hr As New AuthService()
        For i As Integer = 1 To 4
            Dim attempt As Integer = i
            TestRunner.Check("lockout: bad password " & attempt & " rejected", Function() hr.Login(hrName, "wrong-" & attempt).Status = LoginStatus.BadCredentials)
        Next
        TestRunner.Check("lockout: 5th bad attempt locks the account", Function() hr.Login(hrName, "wrong-5").Status = LoginStatus.Locked)
        TestRunner.Check("lockout: correct password refused while locked", Function() hr.Login(hrName, HR_PWD).Status = LoginStatus.Locked)

        ' admin unlocks
        Dim admin As New AuthService()
        TestRunner.Check("admin: sign in again", Function() admin.Login("admin", adminPassword).IsSuccess)
        admin.CompleteLogin(companyID)
        admin.UnlockUser(hrID)
        admin.Logout()

        Dim outcome As LoginOutcome = hr.Login(hrName, HR_PWD)
        TestRunner.Check("lockout: works again after unlock", Function() outcome.IsSuccess)
        TestRunner.Check("password: new user must change it", Function() outcome.MustChangePassword)
        hr.CompleteLogin(companyID)
        TestRunner.Check("session: HR user has company and permissions", Function() AppSession.Current.CompanyID.HasValue AndAlso AppSession.Current.Has("EMPLOYEE_EDIT"))
        TestRunner.Check("session: HR user lacks sensitive/admin rights", Function() Not AppSession.Current.Has("EMPLOYEE_SENSITIVE") AndAlso Not AppSession.Current.Has("USER_ADMIN"))

        TestRunner.Expect(Of BusinessException)("password: wrong current refused", Sub() hr.ChangePassword("not-my-password1", HR_NEW))
        TestRunner.Expect(Of BusinessException)("password: same password refused", Sub() hr.ChangePassword(HR_PWD, HR_PWD))
        hr.ChangePassword(HR_PWD, HR_NEW)
        TestRunner.Check("password: changed, force flag cleared", Function() Not AppSession.Current.MustChangePassword)

        ' permission limits for the HR user
        Dim emps As New EmployeeService(crypto)
        Dim company As New CompanyService()
        TestRunner.Expect(Of BusinessException)("permission: HR cannot save Aadhaar/PAN", Sub() emps.Save(New EmployeeRecord With {.Name = "Nope", .DOJ = New Date(2024, 1, 1)}, New EmployeeSecrets With {.Aadhaar = AADHAAR}))
        TestRunner.Expect(Of BusinessException)("permission: HR cannot create companies", Sub() company.Create(New CompanyInfo With {.Name = "Nope Co"}))
        TestRunner.Expect(Of BusinessException)("permission: HR cannot list users", Sub() hr.ListUsers(Nothing))
        Dim attSvc As New AttendanceService()
        TestRunner.Expect(Of BusinessException)("permission: HR cannot close a month", Sub() attSvc.CloseMonth(2025, 1))
        TestRunner.Expect(Of BusinessException)("permission: HR cannot open another company", Sub() hr.CompleteLogin(otherCompanyID))
        TestRunner.Check("permission: HR still sees own company after refused switch", Function() emps.Search(Nothing, Nothing, Nothing, True, 1, 10) IsNot Nothing)
        hr.Logout()

        TestRunner.Check("password: old password no longer works", Function() hr.Login(hrName, HR_PWD).Status = LoginStatus.BadCredentials)
        TestRunner.Check("password: new password works", Function() hr.Login(hrName, HR_NEW).IsSuccess)
        hr.Logout()

        ' disable
        TestRunner.Check("admin: sign in for disable test", Function() admin.Login("admin", adminPassword).IsSuccess)
        admin.CompleteLogin(companyID)
        TestRunner.Expect(Of BusinessException)("user: admin cannot disable own account", Sub() admin.SetUserActive(AppSession.Current.UserID, False))
        admin.SetUserActive(hrID, False)
        admin.Logout()
        TestRunner.Check("user: disabled account refused", Function() hr.Login(hrName, HR_NEW).Status = LoginStatus.Inactive)
    End Sub

    Private Shared Function FindRole(auth As AuthService, name As String) As Integer
        For Each row As DataRow In auth.ListRoles().Rows
            If String.Equals(Convert.ToString(row("RoleName")), name, StringComparison.OrdinalIgnoreCase) Then Return Convert.ToInt32(row("RoleID"))
        Next
        Return 0
    End Function

    ''' <summary>Reference lists, division, factory division, company detail, employee detail.</summary>
    Private Shared Sub DetailTests(company As CompanyService, masters As MasterService, emps As EmployeeService, empID As Integer, factoryID As Integer)
        TestRunner.Section("Reference data and detail records")
        Dim refs As New ReferenceService()
        Dim stateId As Integer = FindId(refs.States(), "StateID", "Haryana")
        Dim rohtakId As Integer = FindId(refs.Districts(stateId), "DistrictID", "Rohtak")
        Dim bankId As Integer = FindId(refs.Banks(), "BankID", "State Bank of India")
        TestRunner.Check("reference: 36 states and UTs seeded", Function() refs.States().Rows.Count >= 36)
        TestRunner.Check("reference: Haryana has 22 districts", Function() refs.Districts(stateId).Rows.Count = 22)
        TestRunner.Check("reference: banks seeded", Function() refs.Banks().Rows.Count >= 20 AndAlso bankId > 0)

        Dim divID As Integer = EnsureLookup(masters.Divisions(False), "DivisionID", "Smoke Division", Function(n) masters.SaveDivision(Nothing, n))
        TestRunner.Check("division: created", Function() divID > 0)
        TestRunner.Expect(Of BusinessException)("division: duplicate refused", Sub() masters.SaveDivision(Nothing, "Smoke Division"))
        masters.SaveFactory(New FactoryInfo With {.FactoryID = factoryID, .Name = "Smoke Factory", .Zone = "Zone A", .LicenseNo = "HR/TEST/1", .DivisionID = divID})
        TestRunner.Check("factory: linked to division", Function()
                                                            For Each row As DataRow In masters.Factories(False).Rows
                                                                If Convert.ToInt32(row("FactoryID")) = factoryID Then Return Convert.ToString(row("Division")) = "Smoke Division"
                                                            Next
                                                            Return False
                                                        End Function)

        Dim cd As New CompanyDetail With {
            .Tagline = "Smoke tagline", .StartDate = New Date(2020, 4, 1), .StateID = stateId, .DistrictID = rohtakId, .PIN = "124001",
            .Phone = "01262-123456", .Mobile = "9876543210", .Email = "hr@example.com", .Website = "www.example.com",
            .GstNo = "06ABCDE1234F1Z5", .GstDate = New Date(2019, 7, 1), .EpfoDate = New Date(2020, 5, 1),
            .LabourLicenseNo = "LL/1", .LabourFrom = New Date(2024, 1, 1), .LabourTo = New Date(2024, 12, 31),
            .BankID = bankId, .BankAccount = "123456789012", .BankIfsc = "SBIN0001234", .BankBranchAddress = "Rohtak main"}
        company.SaveDetail(cd)
        Dim back As CompanyDetail = company.GetDetail()
        TestRunner.Check("company detail: round trip", Function() back.StateID.GetValueOrDefault() = stateId AndAlso back.DistrictID.GetValueOrDefault() = rohtakId AndAlso back.PIN = "124001" AndAlso back.BankID.GetValueOrDefault() = bankId AndAlso back.LabourTo.GetValueOrDefault() = New Date(2024, 12, 31) AndAlso back.Email = "hr@example.com")
        TestRunner.Expect(Of BusinessException)("company detail: bad PIN refused", Sub() company.SaveDetail(New CompanyDetail With {.PIN = "12"}))
        TestRunner.Expect(Of BusinessException)("company detail: bad email refused", Sub() company.SaveDetail(New CompanyDetail With {.Email = "not-an-email"}))
        TestRunner.Expect(Of BusinessException)("company detail: licence end before start refused", Sub() company.SaveDetail(New CompanyDetail With {.LabourFrom = New Date(2024, 5, 1), .LabourTo = New Date(2024, 1, 1)}))
        company.SaveDetail(cd)

        Dim empty As EmployeeDetail = emps.LoadDetail(empID)
        TestRunner.Check("employee detail: defaults when none saved", Function() empty.PaymentType = 1 AndAlso empty.EmploymentType = 1 AndAlso Not empty.EpfHigher)
        Dim ed As New EmployeeDetail With {
            .MotherName = "Smoke Mother", .BirthPlace = "Rohtak", .MaritalStatus = "M", .Height = "5'8""", .IdentityMark = "Mole on chin",
            .Mobile2 = "9123456780", .Email = "emp@example.com", .PresentStateID = stateId, .PresentDistrictID = rohtakId, .PresentPIN = "124001",
            .PermAddress = "Village X", .PermStateID = stateId, .PermDistrictID = rohtakId, .PermPIN = "124001",
            .EduStandard = "10th", .EduBoard = "HBSE", .EduPassYear = "2008", .EduPercent = 61.5D,
            .UanDoj = New Date(2024, 4, 1), .EpfMemberId = "HRGGN12345", .EpfHigher = True, .EsicDoj = New Date(2024, 4, 1),
            .LabourId = "LID1234567", .PanName = "SMOKE EMPLOYEE", .AadhaarName = "Smoke Employee",
            .BankID = bankId, .PaymentType = 2, .EmploymentType = 3}
        emps.SaveDetail(empID, ed)
        Dim eb As EmployeeDetail = emps.LoadDetail(empID)
        TestRunner.Check("employee detail: round trip", Function() eb.MotherName = "Smoke Mother" AndAlso eb.MaritalStatus = "M" AndAlso eb.PresentDistrictID.GetValueOrDefault() = rohtakId AndAlso eb.EduPercent.GetValueOrDefault() = 61.5D AndAlso eb.EpfHigher AndAlso eb.PaymentType = 2 AndAlso eb.EmploymentType = 3 AndAlso eb.BankID.GetValueOrDefault() = bankId)
        emps.SaveDetail(empID, ed)
        TestRunner.Expect(Of BusinessException)("employee detail: bad marital status refused", Sub() emps.SaveDetail(empID, New EmployeeDetail With {.MaritalStatus = "X"}))
        TestRunner.Expect(Of BusinessException)("employee detail: bad PIN refused", Sub() emps.SaveDetail(empID, New EmployeeDetail With {.PresentPIN = "abc"}))
        TestRunner.Expect(Of BusinessException)("employee detail: percentage over 100 refused", Sub() emps.SaveDetail(empID, New EmployeeDetail With {.EduPercent = 120D}))
    End Sub

    Private Shared Function FindId(table As DataTable, idColumn As String, name As String) As Integer
        For Each row As DataRow In table.Rows
            If String.Equals(Convert.ToString(row("Name")), name, StringComparison.OrdinalIgnoreCase) Then Return Convert.ToInt32(row(idColumn))
        Next
        Return 0
    End Function

    ''' <summary>Monthly attendance save/reload, month close and reopen.</summary>
    Private Shared Sub AttendanceDbTests(empID As Integer, empCode As String)
        TestRunner.Section("Monthly attendance")
        Dim att As New AttendanceService()
        Const Y As Integer = 2025
        Const M As Integer = 1

        Dim before As DataTable = att.Load(2024, 1, Nothing)
        TestRunner.Check("attendance: employee not listed before joining", Function() FindRow(before, empID) Is Nothing)
        Dim sheet As DataTable = att.Load(Y, M, Nothing)
        Dim row As DataRow = FindRow(sheet, empID)
        TestRunner.Check("attendance: employee listed with zeros, not yet saved", Function() row IsNot Nothing AndAlso Not Convert.ToBoolean(row("HasRow")) AndAlso Convert.ToDecimal(row("WorkingDays")) = 0D)

        ' month may be closed from a previous run: reopen first
        If att.GetStatus(Y, M).IsClosed Then att.ReopenMonth(Y, M, "Smoke test cleanup from an earlier run")
        Dim entry As New AttendanceEntry With {.EmployeeID = empID, .EmpCode = empCode, .WorkingDays = 24D, .Holidays = 4D, .CasualLeave = 1D, .OTHours = 3.5D, .Remarks = "smoke"}
        TestRunner.Check("attendance: save one row", Function() att.Save(Y, M, New List(Of AttendanceEntry) From {entry}) = 1)
        Dim again As DataRow = FindRow(att.Load(Y, M, Nothing), empID)
        TestRunner.Check("attendance: saved values come back", Function() Convert.ToBoolean(again("HasRow")) AndAlso Convert.ToDecimal(again("WorkingDays")) = 24D AndAlso Convert.ToDecimal(again("Holidays")) = 4D AndAlso Convert.ToDecimal(again("CasualLeave")) = 1D AndAlso Convert.ToDecimal(again("OTHours")) = 3.5D AndAlso Convert.ToString(again("Remarks")) = "smoke")
        entry.WorkingDays = 22.5D
        att.Save(Y, M, New List(Of AttendanceEntry) From {entry})
        TestRunner.Check("attendance: update in place (half day)", Function() Convert.ToDecimal(FindRow(att.Load(Y, M, Nothing), empID)("WorkingDays")) = 22.5D)
        TestRunner.Expect(Of BusinessException)("attendance: more days than the month refused", Sub() att.Save(Y, M, New List(Of AttendanceEntry) From {New AttendanceEntry With {.EmployeeID = empID, .EmpCode = empCode, .WorkingDays = 31D, .Holidays = 1D}}))
        TestRunner.Expect(Of BusinessException)("attendance: quarter day refused", Sub() att.Save(Y, M, New List(Of AttendanceEntry) From {New AttendanceEntry With {.EmployeeID = empID, .EmpCode = empCode, .WorkingDays = 20.25D}}))

        TestRunner.Check("month close: open at first", Function() Not att.GetStatus(Y, M).IsClosed)
        att.CloseMonth(Y, M)
        TestRunner.Check("month close: closed with who and when", Function() att.GetStatus(Y, M).IsClosed AndAlso att.GetStatus(Y, M).ClosedOn.HasValue AndAlso Not String.IsNullOrEmpty(att.GetStatus(Y, M).ClosedByName))
        TestRunner.Expect(Of BusinessException)("month close: saving a closed month refused", Sub() att.Save(Y, M, New List(Of AttendanceEntry) From {entry}))
        TestRunner.Expect(Of BusinessException)("month close: reopen needs a reason", Sub() att.ReopenMonth(Y, M, "short"))
        att.ReopenMonth(Y, M, "Smoke test: correcting attendance")
        Dim st As MonthStatus = att.GetStatus(Y, M)
        TestRunner.Check("month close: reopened, reason kept", Function() Not st.IsClosed AndAlso st.ReopenReason = "Smoke test: correcting attendance")
        TestRunner.Check("month close: saving works again", Function() att.Save(Y, M, New List(Of AttendanceEntry) From {entry}) = 1)
        TestRunner.Expect(Of BusinessException)("month close: future month refused", Sub() att.CloseMonth(Date.Today.Year + 1, 1))
        TestRunner.Expect(Of BusinessException)("month close: reopening an open month refused", Sub() att.ReopenMonth(Y, M, "Smoke test: not closed at all"))
    End Sub

    Private Shared Function FindRow(table As DataTable, employeeID As Integer) As DataRow
        For Each r As DataRow In table.Rows
            If Convert.ToInt32(r("EmployeeID")) = employeeID Then Return r
        Next
        Return Nothing
    End Function

    Private Shared Function FindCompany(auth As AuthService, name As String) As Integer
        For Each row As DataRow In auth.GetCompanies().Rows
            If String.Equals(Convert.ToString(row("Name")), name, StringComparison.OrdinalIgnoreCase) Then Return Convert.ToInt32(row("CompanyID"))
        Next
        Return 0
    End Function

    Private Shared Function EnsureFactory(masters As MasterService, name As String) As Integer
        For Each row As DataRow In masters.Factories(False).Rows
            If String.Equals(Convert.ToString(row("Name")), name, StringComparison.OrdinalIgnoreCase) Then Return Convert.ToInt32(row("FactoryID"))
        Next
        Return masters.SaveFactory(New FactoryInfo With {.Name = name, .Zone = "Zone A", .LicenseNo = "HR/TEST/1"})
    End Function

    Private Shared Function EnsureLookup(existing As DataTable, idColumn As String, name As String, create As Func(Of String, Integer)) As Integer
        For Each row As DataRow In existing.Rows
            If String.Equals(Convert.ToString(row("Name")), name, StringComparison.OrdinalIgnoreCase) Then Return Convert.ToInt32(row(idColumn))
        Next
        Return create(name)
    End Function
End Class
