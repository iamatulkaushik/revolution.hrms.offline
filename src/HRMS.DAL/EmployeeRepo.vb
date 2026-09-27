Option Strict On
Option Explicit On

Imports System
Imports HRMS.Common
Imports HRMS.Models
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient

Public NotInheritable Class EmployeeRepo
    Private Sub New()
    End Sub

    Public Shared Function List(search As String, factoryID As Integer?, deptID As Integer?,
                                activeOnly As Boolean, pageNo As Integer, pageSize As Integer) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Employee_List", Db.P("@Search", search), Db.P("@FactoryID", factoryID),
                Db.P("@DeptID", deptID), Db.P("@ActiveOnly", activeOnly), Db.P("@PageNo", pageNo), Db.P("@PageSize", pageSize))
        End Using
    End Function

    ''' <summary>includeSensitive=True asks for the encrypted columns (needs EMPLOYEE_SENSITIVE; audited as REVEAL).</summary>
    Public Shared Function GetByID(employeeID As Integer, includeSensitive As Boolean) As EmployeeRecord
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "mst.usp_Employee_Get", Db.P("@EmployeeID", employeeID), Db.P("@IncludeSensitive", includeSensitive))
            If dt.Rows.Count = 0 Then Return Nothing
            Dim r As DataRow = dt.Rows(0)
            Dim rec As New EmployeeRecord With {
                .EmployeeID = Convert.ToInt32(r("EmployeeID")),
                .CompanyID = Convert.ToInt32(r("CompanyID")),
                .EmpCode = Convert.ToString(r("EmpCode")),
                .Name = Convert.ToString(r("Name")),
                .FatherName = Db.StrOrNull(r("FatherName")),
                .DOB = Db.DateOrNull(r("DOB")),
                .Gender = Db.StrOrNull(r("Gender")),
                .DOJ = Convert.ToDateTime(r("DOJ")),
                .DOL = Db.DateOrNull(r("DOL")),
                .FactoryID = Db.IntOrNull(r("FactoryID")),
                .DeptID = Db.IntOrNull(r("DeptID")),
                .DesigID = Db.IntOrNull(r("DesigID")),
                .SkillCategory = Db.StrOrNull(r("SkillCategory")),
                .IFSC = Db.StrOrNull(r("IFSC")),
                .Mobile = Db.StrOrNull(r("Mobile")),
                .Address = Db.StrOrNull(r("Address")),
                .IsActive = Convert.ToBoolean(r("IsActive")),
                .SensitiveVisible = Convert.ToBoolean(r("SensitiveVisible")),
                .HasAadhaar = Convert.ToBoolean(r("HasAadhaar")),
                .HasPAN = Convert.ToBoolean(r("HasPAN")),
                .HasBankAcc = Convert.ToBoolean(r("HasBankAcc")),
                .HasUAN = Convert.ToBoolean(r("HasUAN")),
                .HasEsiIP = Convert.ToBoolean(r("HasEsiIP"))
            }
            rec.Cipher.AadhaarEnc = Db.BytesOrNull(r("AadhaarEnc"))
            rec.Cipher.PANEnc = Db.BytesOrNull(r("PANEnc"))
            rec.Cipher.BankAccEnc = Db.BytesOrNull(r("BankAccEnc"))
            rec.Cipher.UANEnc = Db.BytesOrNull(r("UANEnc"))
            rec.Cipher.EsiIPEnc = Db.BytesOrNull(r("EsiIPEnc"))
            Return rec
        End Using
    End Function

    ''' <summary>Creates the employee; sets rec.EmployeeID and rec.EmpCode (auto code when blank).</summary>
    Public Shared Function Create(rec As EmployeeRecord) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Dim args As New List(Of SqlParameter)()
            args.Add(Db.P("@EmpCode", rec.EmpCode))
            args.AddRange(CommonParams(rec))
            Dim dt As DataTable = Db.Table(cn, "mst.usp_Employee_Create", args.ToArray())
            rec.EmployeeID = Convert.ToInt32(dt.Rows(0)("EmployeeID"))
            rec.EmpCode = Convert.ToString(dt.Rows(0)("EmpCode"))
            Return rec.EmployeeID
        End Using
    End Function

    Public Shared Sub Update(rec As EmployeeRecord)
        Using cn As SqlConnection = Db.OpenSession()
            Dim args As New List(Of SqlParameter)()
            args.Add(Db.P("@EmployeeID", rec.EmployeeID))
            args.AddRange(CommonParams(rec))
            Db.Exec(cn, "mst.usp_Employee_Update", args.ToArray())
        End Using
    End Sub

    Public Shared Sub ExitEmployee(employeeID As Integer, dol As Date)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Employee_Exit", Db.P("@EmployeeID", employeeID), Db.P("@DOL", dol))
        End Using
    End Sub

    Public Shared Sub Reactivate(employeeID As Integer)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Employee_Reactivate", Db.P("@EmployeeID", employeeID))
        End Using
    End Sub

    ' ---------- Detail (family, address, education, statutory) ----------
    Public Shared Function GetDetail(employeeID As Integer) As EmployeeDetail
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "mst.usp_EmployeeDetail_Get", Db.P("@EmployeeID", employeeID))
            Dim d As New EmployeeDetail()
            If dt.Rows.Count = 0 Then Return d
            Dim r As DataRow = dt.Rows(0)
            d.MotherName = Db.StrOrNull(r("MotherName"))
            d.BirthPlace = Db.StrOrNull(r("BirthPlace"))
            d.MaritalStatus = Db.StrOrNull(r("MaritalStatus"))
            d.Height = Db.StrOrNull(r("Height"))
            d.IdentityMark = Db.StrOrNull(r("IdentityMark"))
            d.Mobile2 = Db.StrOrNull(r("Mobile2"))
            d.Email = Db.StrOrNull(r("Email"))
            d.Email2 = Db.StrOrNull(r("Email2"))
            d.PresentStateID = Db.IntOrNull(r("PresentStateID"))
            d.PresentDistrictID = Db.IntOrNull(r("PresentDistrictID"))
            d.PresentPIN = Db.StrOrNull(r("PresentPIN"))
            d.PermAddress = Db.StrOrNull(r("PermAddress"))
            d.PermStateID = Db.IntOrNull(r("PermStateID"))
            d.PermDistrictID = Db.IntOrNull(r("PermDistrictID"))
            d.PermPIN = Db.StrOrNull(r("PermPIN"))
            d.EduStandard = Db.StrOrNull(r("EduStandard"))
            d.EduBoard = Db.StrOrNull(r("EduBoard"))
            d.EduPassYear = Db.StrOrNull(r("EduPassYear"))
            d.EduPercent = If(r.IsNull("EduPercent"), CType(Nothing, Decimal?), Convert.ToDecimal(r("EduPercent")))
            d.EduRemarks = Db.StrOrNull(r("EduRemarks"))
            d.UanDoj = Db.DateOrNull(r("UanDoj"))
            d.EpfMemberId = Db.StrOrNull(r("EpfMemberId"))
            d.EpfHigher = Convert.ToBoolean(r("EpfHigher"))
            d.EsicDoj = Db.DateOrNull(r("EsicDoj"))
            d.EsicExitReason = Db.StrOrNull(r("EsicExitReason"))
            d.LabourId = Db.StrOrNull(r("LabourId"))
            d.PanName = Db.StrOrNull(r("PanName"))
            d.AadhaarName = Db.StrOrNull(r("AadhaarName"))
            d.BankID = Db.IntOrNull(r("BankID"))
            d.PaymentType = Convert.ToInt32(r("PaymentType"))
            d.EmploymentType = Convert.ToInt32(r("EmploymentType"))
            Return d
        End Using
    End Function

    Public Shared Sub SaveDetail(employeeID As Integer, d As EmployeeDetail)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_EmployeeDetail_Save", Db.P("@EmployeeID", employeeID),
                Db.P("@MotherName", d.MotherName), Db.P("@BirthPlace", d.BirthPlace), Db.P("@MaritalStatus", d.MaritalStatus),
                Db.P("@Height", d.Height), Db.P("@IdentityMark", d.IdentityMark),
                Db.P("@Mobile2", d.Mobile2), Db.P("@Email", d.Email), Db.P("@Email2", d.Email2),
                Db.P("@PresentStateID", d.PresentStateID), Db.P("@PresentDistrictID", d.PresentDistrictID), Db.P("@PresentPIN", d.PresentPIN),
                Db.P("@PermAddress", d.PermAddress), Db.P("@PermStateID", d.PermStateID), Db.P("@PermDistrictID", d.PermDistrictID), Db.P("@PermPIN", d.PermPIN),
                Db.P("@EduStandard", d.EduStandard), Db.P("@EduBoard", d.EduBoard), Db.P("@EduPassYear", d.EduPassYear),
                Db.P("@EduPercent", d.EduPercent), Db.P("@EduRemarks", d.EduRemarks),
                Db.P("@UanDoj", d.UanDoj), Db.P("@EpfMemberId", d.EpfMemberId), Db.P("@EpfHigher", d.EpfHigher),
                Db.P("@EsicDoj", d.EsicDoj), Db.P("@EsicExitReason", d.EsicExitReason), Db.P("@LabourId", d.LabourId),
                Db.P("@PanName", d.PanName), Db.P("@AadhaarName", d.AadhaarName),
                Db.P("@BankID", d.BankID), Db.P("@PaymentType", d.PaymentType), Db.P("@EmploymentType", d.EmploymentType))
        End Using
    End Sub

    ' ---------- Nominees ----------
    Public Shared Function NomineeList(employeeID As Integer) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Nominee_List", Db.P("@EmployeeID", employeeID))
        End Using
    End Function

    Public Shared Function NomineeSave(n As NomineeInfo) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_Nominee_Save", Db.P("@NomineeID", n.NomineeID),
                Db.P("@EmployeeID", n.EmployeeID), Db.P("@Name", n.Name), Db.P("@Relation", n.Relation),
                Db.P("@DOB", n.DOB), Db.P("@SharePct", n.SharePct)))
        End Using
    End Function

    Public Shared Sub NomineeDelete(nomineeID As Integer)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Nominee_Delete", Db.P("@NomineeID", nomineeID))
        End Using
    End Sub

    ' ---------- internals ----------
    Private Shared Function CommonParams(rec As EmployeeRecord) As SqlParameter()
        Return New SqlParameter() {
            Db.P("@Name", rec.Name), Db.P("@FatherName", rec.FatherName), Db.P("@DOB", rec.DOB),
            Db.P("@Gender", rec.Gender), Db.P("@DOJ", rec.DOJ), Db.P("@FactoryID", rec.FactoryID),
            Db.P("@DeptID", rec.DeptID), Db.P("@DesigID", rec.DesigID), Db.P("@SkillCategory", rec.SkillCategory),
            Db.PBin("@AadhaarEnc", rec.Cipher.AadhaarEnc), Db.PBin("@PANEnc", rec.Cipher.PANEnc),
            Db.PBin("@BankAccEnc", rec.Cipher.BankAccEnc), Db.PBin("@UANEnc", rec.Cipher.UANEnc),
            Db.PBin("@EsiIPEnc", rec.Cipher.EsiIPEnc), Db.P("@IFSC", rec.IFSC), Db.P("@Mobile", rec.Mobile),
            Db.P("@Address", rec.Address)}
    End Function
End Class
