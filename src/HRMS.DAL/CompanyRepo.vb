Option Strict On
Option Explicit On

Imports System
Imports HRMS.Common
Imports HRMS.Models
Imports System.Data
Imports Microsoft.Data.SqlClient

Public NotInheritable Class CompanyRepo
    Private Sub New()
    End Sub

    ''' <summary>The company in the current session.</summary>
    Public Shared Function GetCurrent() As CompanyInfo
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "mst.usp_Company_Get")
            If dt.Rows.Count = 0 Then Return Nothing
            Dim r As DataRow = dt.Rows(0)
            Return New CompanyInfo With {
                .CompanyID = Convert.ToInt32(r("CompanyID")),
                .Name = Convert.ToString(r("Name")),
                .Address = Db.StrOrNull(r("Address")),
                .PAN = Db.StrOrNull(r("PAN")),
                .TAN = Db.StrOrNull(r("TAN")),
                .EsiCode = Db.StrOrNull(r("EsiCode")),
                .EpfCode = Db.StrOrNull(r("EpfCode")),
                .LIN = Db.StrOrNull(r("LIN")),
                .IsActive = Convert.ToBoolean(r("IsActive"))
            }
        End Using
    End Function

    ''' <summary>Associate only. Returns the new CompanyID (assigned to the creator).</summary>
    Public Shared Function Create(c As CompanyInfo) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "mst.usp_Company_Create", Params(c)))
        End Using
    End Function

    Public Shared Sub Update(c As CompanyInfo)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_Company_Update", Params(c))
        End Using
    End Sub

    Public Shared Function GetDetail() As CompanyDetail
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "mst.usp_CompanyDetail_Get")
            Dim d As New CompanyDetail()
            If dt.Rows.Count = 0 Then Return d
            Dim r As DataRow = dt.Rows(0)
            d.Tagline = Db.StrOrNull(r("Tagline"))
            d.StartDate = Db.DateOrNull(r("StartDate"))
            d.ShutDate = Db.DateOrNull(r("ShutDate"))
            d.StateID = Db.IntOrNull(r("StateID"))
            d.DistrictID = Db.IntOrNull(r("DistrictID"))
            d.PIN = Db.StrOrNull(r("PIN"))
            d.Phone = Db.StrOrNull(r("Phone"))
            d.Phone2 = Db.StrOrNull(r("Phone2"))
            d.Mobile = Db.StrOrNull(r("Mobile"))
            d.Mobile2 = Db.StrOrNull(r("Mobile2"))
            d.Email = Db.StrOrNull(r("Email"))
            d.Email2 = Db.StrOrNull(r("Email2"))
            d.Website = Db.StrOrNull(r("Website"))
            d.CIN = Db.StrOrNull(r("CIN"))
            d.ShopActNo = Db.StrOrNull(r("ShopActNo"))
            d.ShopActDate = Db.DateOrNull(r("ShopActDate"))
            d.GstNo = Db.StrOrNull(r("GstNo"))
            d.GstDate = Db.DateOrNull(r("GstDate"))
            d.EpfoDate = Db.DateOrNull(r("EpfoDate"))
            d.EsicDate = Db.DateOrNull(r("EsicDate"))
            d.LabourLicenseNo = Db.StrOrNull(r("LabourLicenseNo"))
            d.LabourFrom = Db.DateOrNull(r("LabourFrom"))
            d.LabourTo = Db.DateOrNull(r("LabourTo"))
            d.PsaraNo = Db.StrOrNull(r("PsaraNo"))
            d.PsaraFrom = Db.DateOrNull(r("PsaraFrom"))
            d.PsaraTo = Db.DateOrNull(r("PsaraTo"))
            d.BankID = Db.IntOrNull(r("BankID"))
            d.BankAccount = Db.StrOrNull(r("BankAccount"))
            d.BankIfsc = Db.StrOrNull(r("BankIfsc"))
            d.BankBranchAddress = Db.StrOrNull(r("BankBranchAddress"))
            Return d
        End Using
    End Function

    Public Shared Sub SaveDetail(d As CompanyDetail)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "mst.usp_CompanyDetail_Save",
                Db.P("@Tagline", d.Tagline), Db.P("@StartDate", d.StartDate), Db.P("@ShutDate", d.ShutDate),
                Db.P("@StateID", d.StateID), Db.P("@DistrictID", d.DistrictID), Db.P("@PIN", d.PIN),
                Db.P("@Phone", d.Phone), Db.P("@Phone2", d.Phone2), Db.P("@Mobile", d.Mobile), Db.P("@Mobile2", d.Mobile2),
                Db.P("@Email", d.Email), Db.P("@Email2", d.Email2), Db.P("@Website", d.Website), Db.P("@CIN", d.CIN),
                Db.P("@ShopActNo", d.ShopActNo), Db.P("@ShopActDate", d.ShopActDate), Db.P("@GstNo", d.GstNo), Db.P("@GstDate", d.GstDate),
                Db.P("@EpfoDate", d.EpfoDate), Db.P("@EsicDate", d.EsicDate),
                Db.P("@LabourLicenseNo", d.LabourLicenseNo), Db.P("@LabourFrom", d.LabourFrom), Db.P("@LabourTo", d.LabourTo),
                Db.P("@PsaraNo", d.PsaraNo), Db.P("@PsaraFrom", d.PsaraFrom), Db.P("@PsaraTo", d.PsaraTo),
                Db.P("@BankID", d.BankID), Db.P("@BankAccount", d.BankAccount), Db.P("@BankIfsc", d.BankIfsc),
                Db.P("@BankBranchAddress", d.BankBranchAddress))
        End Using
    End Sub

    Private Shared Function Params(c As CompanyInfo) As SqlParameter()
        Return New SqlParameter() {
            Db.P("@Name", c.Name), Db.P("@Address", c.Address), Db.P("@PAN", c.PAN), Db.P("@TAN", c.TAN),
            Db.P("@EsiCode", c.EsiCode), Db.P("@EpfCode", c.EpfCode), Db.P("@LIN", c.LIN)}
    End Function
End Class
