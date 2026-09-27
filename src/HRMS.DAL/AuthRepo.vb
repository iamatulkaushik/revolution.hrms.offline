Option Strict On
Option Explicit On

Imports System
Imports HRMS.Common
Imports HRMS.Models
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient

Public NotInheritable Class AuthRepo
    Private Sub New()
    End Sub

    Public Shared Function BootstrapNeeded() As Boolean
        Using cn As SqlConnection = Db.OpenRaw()
            Return Convert.ToInt32(Db.Scalar(cn, "sec.usp_Bootstrap_Needed")) = 1
        End Using
    End Function

    Public Shared Function BootstrapAdmin(username As String, fullName As String, hash As Byte(), salt As Byte(), iterations As Integer) As Integer
        Using cn As SqlConnection = Db.OpenRaw()
            Return Convert.ToInt32(Db.Scalar(cn, "sec.usp_Bootstrap_CreateAdmin",
                Db.P("@Username", username), Db.P("@FullName", fullName),
                Db.PBin("@PasswordHash", hash), Db.PBin("@Salt", salt), Db.P("@Iterations", iterations)))
        End Using
    End Function

    Public Shared Function GetForLogin(username As String) As LoginRecord
        Using cn As SqlConnection = Db.OpenRaw()
            Dim dt As DataTable = Db.Table(cn, "sec.usp_User_GetForLogin", Db.P("@Username", username))
            If dt.Rows.Count = 0 Then Return Nothing
            Dim r As DataRow = dt.Rows(0)
            Return New LoginRecord With {
                .UserID = Convert.ToInt32(r("UserID")),
                .Username = Convert.ToString(r("Username")),
                .FullName = Convert.ToString(r("FullName")),
                .PasswordHash = DirectCast(r("PasswordHash"), Byte()),
                .Salt = DirectCast(r("Salt"), Byte()),
                .Iterations = Convert.ToInt32(r("Iterations")),
                .RoleID = Convert.ToInt32(r("RoleID")),
                .UserType = Convert.ToString(r("UserType")),
                .HomeCompanyID = Db.IntOrNull(r("HomeCompanyID")),
                .EmployeeID = Db.IntOrNull(r("EmployeeID")),
                .FailedCount = Convert.ToInt32(r("FailedCount")),
                .LockedUntil = Db.DateOrNull(r("LockedUntil")),
                .ServerNow = Convert.ToDateTime(r("ServerNow")),
                .MustChangePwd = Convert.ToBoolean(r("MustChangePwd")),
                .IsActive = Convert.ToBoolean(r("IsActive"))
            }
        End Using
    End Function

    Public Shared Sub RecordLoginResult(userID As Integer, success As Boolean, maxFails As Integer, lockMinutes As Integer)
        Using cn As SqlConnection = Db.OpenRaw()
            Db.Exec(cn, "sec.usp_User_LoginResult", Db.P("@UserID", userID), Db.P("@Success", success),
                    Db.P("@MaxFails", maxFails), Db.P("@LockMinutes", lockMinutes))
        End Using
    End Sub

    Public Shared Function GetCompanies(userID As Integer) As DataTable
        Using cn As SqlConnection = Db.OpenRaw()
            Return Db.Table(cn, "sec.usp_User_GetCompanies", Db.P("@UserID", userID))
        End Using
    End Function

    Public Shared Function GetPermissions(userID As Integer) As List(Of String)
        Dim result As New List(Of String)()
        Using cn As SqlConnection = Db.OpenRaw()
            For Each row As DataRow In Db.Table(cn, "sec.usp_User_GetPermissions", Db.P("@UserID", userID)).Rows
                result.Add(Convert.ToString(row("Code")))
            Next
        End Using
        Return result
    End Function

    Public Shared Sub ChangePassword(userID As Integer, hash As Byte(), salt As Byte(), iterations As Integer)
        Using cn As SqlConnection = Db.OpenRaw()
            Db.Exec(cn, "sec.usp_User_ChangePassword", Db.P("@UserID", userID),
                    Db.PBin("@PasswordHash", hash), Db.PBin("@Salt", salt), Db.P("@Iterations", iterations))
        End Using
    End Sub

    Public Shared Function CreateUser(username As String, fullName As String, hash As Byte(), salt As Byte(), iterations As Integer,
                                      roleID As Integer, homeCompanyID As Integer?, employeeID As Integer?, actorID As Integer) As Integer
        Using cn As SqlConnection = Db.OpenRaw()
            Return Convert.ToInt32(Db.Scalar(cn, "sec.usp_User_Create",
                Db.P("@Username", username), Db.P("@FullName", fullName),
                Db.PBin("@PasswordHash", hash), Db.PBin("@Salt", salt), Db.P("@Iterations", iterations),
                Db.P("@RoleID", roleID), Db.P("@HomeCompanyID", homeCompanyID), Db.P("@EmployeeID", employeeID),
                Db.P("@ActorID", actorID)))
        End Using
    End Function

    Public Shared Function ListUsers(companyID As Integer?) As DataTable
        Using cn As SqlConnection = Db.OpenRaw()
            Return Db.Table(cn, "sec.usp_User_List", Db.P("@CompanyID", companyID))
        End Using
    End Function

    Public Shared Function ListRoles() As DataTable
        Using cn As SqlConnection = Db.OpenRaw()
            Return Db.Table(cn, "sec.usp_Role_List")
        End Using
    End Function

    Public Shared Sub Unlock(userID As Integer, actorID As Integer)
        Using cn As SqlConnection = Db.OpenRaw()
            Db.Exec(cn, "sec.usp_User_Unlock", Db.P("@UserID", userID), Db.P("@ActorID", actorID))
        End Using
    End Sub

    Public Shared Sub SetActive(userID As Integer, isActive As Boolean, actorID As Integer)
        Using cn As SqlConnection = Db.OpenRaw()
            Db.Exec(cn, "sec.usp_User_SetActive", Db.P("@UserID", userID), Db.P("@IsActive", isActive), Db.P("@ActorID", actorID))
        End Using
    End Sub
End Class
