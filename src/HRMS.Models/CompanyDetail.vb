Option Strict On
Option Explicit On

Imports System

''' <summary>Extra company data: contact, statutory registrations, bank (legacy RA.Company columns).</summary>
Public NotInheritable Class CompanyDetail
    Public Property Tagline As String
    Public Property StartDate As Date?
    Public Property ShutDate As Date?
    Public Property StateID As Integer?
    Public Property DistrictID As Integer?
    Public Property PIN As String
    Public Property Phone As String
    Public Property Phone2 As String
    Public Property Mobile As String
    Public Property Mobile2 As String
    Public Property Email As String
    Public Property Email2 As String
    Public Property Website As String
    Public Property CIN As String
    Public Property ShopActNo As String
    Public Property ShopActDate As Date?
    Public Property GstNo As String
    Public Property GstDate As Date?
    Public Property EpfoDate As Date?
    Public Property EsicDate As Date?
    Public Property LabourLicenseNo As String
    Public Property LabourFrom As Date?
    Public Property LabourTo As Date?
    Public Property PsaraNo As String
    Public Property PsaraFrom As Date?
    Public Property PsaraTo As Date?
    Public Property BankID As Integer?
    Public Property BankAccount As String
    Public Property BankIfsc As String
    Public Property BankBranchAddress As String
End Class
