Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Compression
Imports System.Security.Cryptography
Imports HRMS.BLL
Imports HRMS.Common
Imports HRMS.Models

''' <summary>Offline tests: no database needed.</summary>
Public NotInheritable Class SelfTests
    Private Const TEST_AADHAAR As String = "234123412346"   ' Verhoeff-valid sample

    Private Sub New()
    End Sub

    Public Shared Sub Run()
        TestRunner.Section("Offline self-tests (no database)")
        HashingTests()
        CryptoTests()
        KeyStoreTests()
        FormatTests()
        ValidatorTests()
        SessionTests()
        AttendanceTests()
        FileTests()
    End Sub

    Private Shared Sub HashingTests()
        Dim salt As Byte() = PasswordHasher.NewSalt()
        Dim hash As Byte() = PasswordHasher.Hash("Tr1cky-Pass", salt, 20000)
        TestRunner.Check("hash: correct password verifies", Function() PasswordHasher.Verify("Tr1cky-Pass", salt, 20000, hash))
        TestRunner.Check("hash: wrong password rejected", Function() Not PasswordHasher.Verify("tr1cky-pass", salt, 20000, hash))
        TestRunner.Check("hash: 32-byte output, 32-byte salt", Function() hash.Length = 32 AndAlso salt.Length = 32)
        TestRunner.Check("hash: salts differ between calls", Function() Not ByteUtil.ConstantTimeEquals(salt, PasswordHasher.NewSalt()))
        TestRunner.Check("policy: short password rejected", Function() PasswordPolicy.Validate("ab1", "bob") IsNot Nothing)
        TestRunner.Check("policy: no digit rejected", Function() PasswordPolicy.Validate("onlyletters", "bob") IsNot Nothing)
        TestRunner.Check("policy: common password rejected", Function() PasswordPolicy.Validate("Password1", "bob") IsNot Nothing)
        TestRunner.Check("policy: good password accepted", Function() PasswordPolicy.Validate("Tr1cky-Pass", "bob") Is Nothing)
        TestRunner.Check("bytes: constant-time compare", Function() ByteUtil.ConstantTimeEquals(New Byte() {1, 2}, New Byte() {1, 2}) AndAlso Not ByteUtil.ConstantTimeEquals(New Byte() {1, 2}, New Byte() {1, 3}))
    End Sub

    Private Shared Sub CryptoTests()
        Dim master(63) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(master)
        End Using
        Dim fc As New FieldCrypto(master)
        Dim blob As Byte() = fc.EncryptText(FieldCrypto.PURPOSE_AADHAAR, TEST_AADHAAR)
        TestRunner.Check("crypto: round trip", Function() fc.DecryptText(FieldCrypto.PURPOSE_AADHAAR, blob) = TEST_AADHAAR)
        TestRunner.Check("crypto: fits VARBINARY(256)", Function() blob.Length <= 256)
        TestRunner.Info("ciphertext length for 12 chars = " & blob.Length & " bytes")
        TestRunner.Check("crypto: same text encrypts differently (random IV)", Function() Not ByteUtil.ConstantTimeEquals(blob, fc.EncryptText(FieldCrypto.PURPOSE_AADHAAR, TEST_AADHAAR)))
        TestRunner.Check("crypto: empty value -> Nothing", Function() fc.EncryptText(FieldCrypto.PURPOSE_PAN, "  ") Is Nothing)
        TestRunner.Check("crypto: Nothing cipher -> Nothing", Function() fc.DecryptText(FieldCrypto.PURPOSE_PAN, Nothing) Is Nothing)
        TestRunner.Expect(Of CryptographicException)("crypto: wrong purpose rejected", Sub() fc.DecryptText(FieldCrypto.PURPOSE_PAN, blob))
        Dim tampered As Byte() = DirectCast(blob.Clone(), Byte())
        tampered(20) = CByte(tampered(20) Xor &H1)
        TestRunner.Expect(Of CryptographicException)("crypto: tampered data rejected", Sub() fc.DecryptText(FieldCrypto.PURPOSE_AADHAAR, tampered))
        Dim other(63) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(other)
        End Using
        Dim fc2 As New FieldCrypto(other)
        TestRunner.Expect(Of CryptographicException)("crypto: wrong key rejected", Sub() fc2.DecryptText(FieldCrypto.PURPOSE_AADHAAR, blob))
        Dim longBank As Byte() = fc.EncryptText(FieldCrypto.PURPOSE_BANK_ACC, "123456789012345678")
        TestRunner.Check("crypto: 18-digit account round trip", Function() fc.DecryptText(FieldCrypto.PURPOSE_BANK_ACC, longBank) = "123456789012345678")
    End Sub

    Private Shared Sub KeyStoreTests()
        Dim dir As String = Path.Combine(Path.GetTempPath(), "hrms-selftest-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(dir)
        Try
            Dim a As New KeyStore(New MemoryKeyProtector(), Path.Combine(dir, "a.key"))
            Dim b As New KeyStore(New MemoryKeyProtector(), Path.Combine(dir, "b.key"))
            a.CreateNew()
            TestRunner.Check("keystore: create + load 64 bytes", Function() a.Exists() AndAlso a.Load().Length = 64)
            TestRunner.Expect(Of BusinessException)("keystore: second create refused", Sub() a.CreateNew())
            Dim exportPath As String = Path.Combine(dir, "export.hrk")
            TestRunner.Expect(Of BusinessException)("keystore: short passphrase refused", Sub() a.ExportToFile(exportPath, "short"))
            a.ExportToFile(exportPath, "correct horse battery")
            TestRunner.Expect(Of BusinessException)("keystore: wrong passphrase rejected", Sub() b.ImportFromFile(exportPath, "wrong passphrase!!"))
            b.ImportFromFile(exportPath, "correct horse battery")
            TestRunner.Check("keystore: imported key equals original", Function() ByteUtil.ConstantTimeEquals(a.Load(), b.Load()))
            TestRunner.Check("keystore: cross-PC decrypt works", Function() New FieldCrypto(b.Load()).DecryptText(FieldCrypto.PURPOSE_UAN, New FieldCrypto(a.Load()).EncryptText(FieldCrypto.PURPOSE_UAN, "123456789012")) = "123456789012")
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    Private Shared Sub FormatTests()
        Dim rs As String = IndianFormat.RUPEE_SYMBOL
        TestRunner.Check("rupees: 1234567.5", Function() IndianFormat.Rupees(1234567.5D) = rs & " 12,34,567.50")
        TestRunner.Check("rupees: 0", Function() IndianFormat.Rupees(0D) = rs & " 0.00")
        TestRunner.Check("rupees: 999.995 rounds up", Function() IndianFormat.Rupees(999.995D) = rs & " 1,000.00")
        TestRunner.Check("rupees: 100000 (lakh)", Function() IndianFormat.Rupees(100000D, False) = "1,00,000.00")
        TestRunner.Check("rupees: crore", Function() IndianFormat.Rupees(123456789D, False) = "12,34,56,789.00")
        TestRunner.Check("rupees: negative", Function() IndianFormat.Rupees(-1500.5D) = "-" & rs & " 1,500.50")
        TestRunner.Check("mask: keeps last 4", Function() Masker.Mask("123456789012") = "XXXXXXXX9012")
        TestRunner.Check("fy: Sep 2026 -> 2026-27", Function() FinancialYear.FyName(New Date(2026, 9, 20)) = "2026-27")
        TestRunner.Check("fy: Feb 2027 -> 2026-27", Function() FinancialYear.FyName(New Date(2027, 2, 1)) = "2026-27")
        TestRunner.Check("fy: bounds", Function() FinancialYear.FromDate(New Date(2027, 2, 1)) = New Date(2026, 4, 1) AndAlso FinancialYear.ToDate(New Date(2027, 2, 1)) = New Date(2027, 3, 31))
    End Sub

    Private Shared Sub ValidatorTests()
        TestRunner.Check("validate: PAN ok", Function() Validators.IsValidPan("ABCDE1234F"))
        TestRunner.Check("validate: PAN lowercase rejected", Function() Not Validators.IsValidPan("abcde1234f"))
        TestRunner.Check("validate: Aadhaar checksum ok", Function() Validators.IsValidAadhaar(TEST_AADHAAR))
        TestRunner.Check("validate: Aadhaar bad checksum", Function() Not Validators.IsValidAadhaar("234123412345"))
        TestRunner.Check("validate: Aadhaar cannot start with 0/1", Function() Not Validators.IsValidAadhaar("134123412346"))
        TestRunner.Check("validate: mobile ok / bad", Function() Validators.IsValidMobile("9876543210") AndAlso Not Validators.IsValidMobile("1234567890"))
        TestRunner.Check("validate: IFSC", Function() Validators.IsValidIfsc("SBIN0001234") AndAlso Not Validators.IsValidIfsc("SBIN1001234"))
        TestRunner.Check("validate: email", Function() Validators.IsValidEmail("hr@example.com") AndAlso Not Validators.IsValidEmail("hr@example") AndAlso Not Validators.IsValidEmail("hr example@x.com") AndAlso Not Validators.IsValidEmail(Nothing))
        TestRunner.Check("validate: UAN / ESI IP / bank", Function() Validators.IsValidUan("123456789012") AndAlso Validators.IsValidEsiIp("1234567890") AndAlso Validators.IsValidBankAccount("123456789"))
    End Sub

    Private Shared Sub AttendanceTests()
        Dim e As New AttendanceEntry With {.EmpCode = "E1", .WorkingDays = 24D, .Holidays = 4D, .CasualLeave = 1D, .SickLeave = 0.5D}
        TestRunner.Check("attendance: days in month", Function() AttendanceEngine.DaysInMonth(2024, 2) = 29 AndAlso AttendanceEngine.DaysInMonth(2026, 2) = 28 AndAlso AttendanceEngine.DaysInMonth(2026, 9) = 30)
        TestRunner.Check("attendance: paid days add up", Function() AttendanceEngine.PaidDays(e) = 29.5D)
        TestRunner.Check("attendance: absent days", Function() AttendanceEngine.AbsentDays(e, 31) = 1.5D AndAlso AttendanceEngine.AbsentDays(e, 28) = 0D)
        TestRunner.Check("attendance: half-day steps", Function() AttendanceEngine.IsHalfStep(2.5D) AndAlso AttendanceEngine.IsHalfStep(0D) AndAlso Not AttendanceEngine.IsHalfStep(2.3D))
        TestRunner.Check("attendance: valid entry has no messages", Function() AttendanceEngine.Validate(e, 2026, 7).Count = 0)
        TestRunner.Check("attendance: too many days for a 30-day month", Function() AttendanceEngine.Validate(New AttendanceEntry With {.WorkingDays = 27D, .Holidays = 4D}, 2026, 9).Count = 1 AndAlso AttendanceEngine.Validate(e, 2026, 9).Count = 0)
        TestRunner.Check("attendance: negative and quarter days refused", Function() AttendanceEngine.Validate(New AttendanceEntry With {.WorkingDays = -1D, .CasualLeave = 0.25D}, 2026, 7).Count = 2)
        TestRunner.Check("attendance: overtime cannot be negative", Function() AttendanceEngine.Validate(New AttendanceEntry With {.OTHours = -2D}, 2026, 7).Count = 1)
        TestRunner.Check("attendance: window for mid-month joiner", Function() AttendanceEngine.PayableWindow(2026, 9, New Date(2026, 9, 10), Nothing) = 21)
        TestRunner.Check("attendance: window for mid-month leaver", Function() AttendanceEngine.PayableWindow(2025, 1, New Date(2026, 9, 15), New Date(2026, 9, 15)) = 0 AndAlso AttendanceEngine.PayableWindow(2026, 9, New Date(2020, 1, 1), New Date(2026, 9, 15)) = 15)
        TestRunner.Check("attendance: window for full month", Function() AttendanceEngine.PayableWindow(2026, 9, New Date(2020, 1, 1), Nothing) = 30)

        Dim ids As New Dictionary(Of String, Integer) From {{"E00001", 1}, {"E00002", 2}, {"E00003", 3}}
        Dim csv As String = "Monthly sheet,,,," & Environment.NewLine &
            "Emp. Code,WD,H D,C.L,OT,Remarks" & Environment.NewLine &
            "E00001,26,4,0,2.5,ok" & Environment.NewLine &
            "e00002,20,4,0.5,," & Environment.NewLine &
            "E00009,26,4,0,0," & Environment.NewLine &
            "E00001,10,0,0,0," & Environment.NewLine &
            "E00003,abc,0,0,0," & Environment.NewLine &
            ",,,," & Environment.NewLine
        Dim res As ImportResult = AttendanceImporter.Parse(TabularFile.ReadCsv(csv), ids, 2026, 9)
        TestRunner.Check("import: two good rows", Function() res.Entries.Count = 2 AndAlso res.Entries(0).EmployeeID = 1 AndAlso res.Entries(0).WorkingDays = 26D AndAlso res.Entries(0).OTHours = 2.5D AndAlso res.Entries(0).Remarks = "ok")
        TestRunner.Check("import: code matched without regard to case, blanks are zero", Function() res.Entries(1).EmployeeID = 2 AndAlso res.Entries(1).CasualLeave = 0.5D AndAlso res.Entries(1).OTHours = 0D)
        TestRunner.Check("import: unknown code, duplicate and bad number reported", Function() res.Errors.Count = 3 AndAlso res.RowsRead = 5)
        TestRunner.Info(String.Join(" | ", res.Errors))
        Dim over As ImportResult = AttendanceImporter.Parse(TabularFile.ReadCsv("Code,WD,HD" & Environment.NewLine & "E00001,28,5"), ids, 2026, 9)
        TestRunner.Check("import: more days than the month refused", Function() over.Entries.Count = 0 AndAlso over.Errors.Count = 1)
        Dim noHeader As ImportResult = AttendanceImporter.Parse(TabularFile.ReadCsv("a,b" & Environment.NewLine & "1,2"), ids, 2026, 9)
        TestRunner.Check("import: missing header explained", Function() noHeader.Errors.Count = 1 AndAlso noHeader.Entries.Count = 0)
        Dim noCols As ImportResult = AttendanceImporter.Parse(TabularFile.ReadCsv("Code,Name" & Environment.NewLine & "E00001,X"), ids, 2026, 9)
        TestRunner.Check("import: no attendance columns explained", Function() noCols.Errors.Count = 1)
    End Sub

    Private Shared Sub FileTests()
        Dim rows As List(Of String()) = TabularFile.ReadCsv("Code;WD;Note" & Environment.NewLine & "E1;26;""said """"hi"""", ok""" & Environment.NewLine)
        TestRunner.Check("csv: semicolon delimiter and quoted text", Function() rows.Count = 2 AndAlso rows(0).Length = 3 AndAlso rows(1)(2) = "said ""hi"", ok")
        TestRunner.Check("csv: tab delimiter", Function() TabularFile.ReadCsv("a" & Convert.ToChar(9) & "b" & Environment.NewLine & "1" & Convert.ToChar(9) & "2")(1)(1) = "2")

        Dim dir As String = Path.Combine(Path.GetTempPath(), "hrms-file-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(dir)
        Try
            Dim csvPath As String = Path.Combine(dir, "t.csv")
            TabularFile.WriteCsv(csvPath, New List(Of String()) From {New String() {"Code", "Remarks"}, New String() {"E1", "a,b ""c"""}})
            Dim back As List(Of String()) = TabularFile.Read(csvPath)
            TestRunner.Check("csv: write then read keeps commas and quotes", Function() back.Count = 2 AndAlso back(1)(1) = "a,b ""c""")

            Dim xlsxPath As String = Path.Combine(dir, "t.xlsx")
            File.WriteAllBytes(xlsxPath, BuildXlsx())
            Dim x As List(Of String()) = TabularFile.Read(xlsxPath)
            TestRunner.Check("xlsx: shared strings and numbers", Function() x(0)(0) = "Code" AndAlso x(0)(1) = "WD" AndAlso x(1)(0) = "E00001" AndAlso x(1)(1) = "26")
            TestRunner.Check("xlsx: rich text cell joined", Function() x(1)(2) = "Rich text")
            TestRunner.Check("xlsx: blank rows and columns keep their position", Function() x.Count = 4 AndAlso x(2).Length = 0 AndAlso x(3).Length = 4 AndAlso x(3)(2) = "hello" AndAlso x(3)(3) = "0.5")
            Dim res As ImportResult = AttendanceImporter.Parse(x, New Dictionary(Of String, Integer) From {{"E00001", 7}}, 2026, 9)
            TestRunner.Check("xlsx: feeds the attendance importer", Function() res.Entries.Count = 1 AndAlso res.Entries(0).EmployeeID = 7 AndAlso res.Entries(0).WorkingDays = 26D)
            TestRunner.Expect(Of BusinessException)("file: old .xls refused", Sub() TabularFile.Read(Path.Combine(dir, "old.xls")))
            File.WriteAllText(Path.Combine(dir, "bad.xlsx"), "not a zip")
            TestRunner.Expect(Of BusinessException)("file: damaged .xlsx explained", Sub() TabularFile.Read(Path.Combine(dir, "bad.xlsx")))
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    Private Shared Function BuildXlsx() As Byte()
        Const MAIN As String = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        Const REL As String = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
        Using ms As New MemoryStream()
            Using zip As New ZipArchive(ms, ZipArchiveMode.Create, True)
                AddEntry(zip, "[Content_Types].xml", "<?xml version=""1.0""?><Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""xml"" ContentType=""application/xml""/></Types>")
                AddEntry(zip, "xl/workbook.xml", "<workbook xmlns=""" & MAIN & """ xmlns:r=""" & REL & """><sheets><sheet name=""Sheet1"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>")
                AddEntry(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""x"" Target=""worksheets/sheet1.xml""/></Relationships>")
                AddEntry(zip, "xl/sharedStrings.xml", "<sst xmlns=""" & MAIN & """><si><t>Code</t></si><si><t>WD</t></si><si><t>E00001</t></si><si><r><t>Rich</t></r><r><t xml:space=""preserve""> text</t></r></si></sst>")
                AddEntry(zip, "xl/worksheets/sheet1.xml", "<worksheet xmlns=""" & MAIN & """><sheetData>" &
                    "<row r=""1""><c r=""A1"" t=""s""><v>0</v></c><c r=""B1"" t=""s""><v>1</v></c></row>" &
                    "<row r=""2""><c r=""A2"" t=""s""><v>2</v></c><c r=""B2""><v>26</v></c><c r=""C2"" t=""s""><v>3</v></c></row>" &
                    "<row r=""4""><c r=""C4"" t=""inlineStr""><is><t>hello</t></is></c><c r=""D4""><v>0.5</v></c></row>" &
                    "</sheetData></worksheet>")
            End Using
            Return ms.ToArray()
        End Using
    End Function

    Private Shared Sub AddEntry(zip As ZipArchive, name As String, content As String)
        Dim entry As ZipArchiveEntry = zip.CreateEntry(name)
        Using w As New StreamWriter(entry.Open(), New System.Text.UTF8Encoding(False))
            w.Write(content)
        End Using
    End Sub

    Private Shared Sub SessionTests()
        Dim s As New UserSession(1, "u", "User", 2, "Company", Nothing, 5, "Co", False, New String() {"EMPLOYEE_VIEW"})
        TestRunner.Check("session: has permission", Function() s.Has("EMPLOYEE_VIEW") AndAlso s.Has("employee_view"))
        TestRunner.Check("session: lacks permission", Function() Not s.Has("EMPLOYEE_EDIT"))
        TestRunner.Expect(Of BusinessException)("session: Require throws", Sub() s.Require("EMPLOYEE_EDIT"))
        Dim assoc As New UserSession(2, "a", "Assoc", 1, "Associate", Nothing, Nothing, Nothing, False, New String() {})
        TestRunner.Expect(Of BusinessException)("session: RequireCompany throws when none", Sub() assoc.RequireCompany())
    End Sub
End Class
