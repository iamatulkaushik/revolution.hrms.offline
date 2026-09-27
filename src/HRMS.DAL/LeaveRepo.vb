Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports HRMS.Models

''' <summary>Leave application workflow (att.Leave) and balance (att.usp_LeaveBalance_Get). The self-vs-HR
''' access rule (an employee may apply/list/cancel only their own leave; approve/reject always needs
''' ATT_EDIT) is enforced inside the stored procs themselves, the same way usp_Employee_Get does it -
''' this repo does not duplicate that check.</summary>
Public NotInheritable Class LeaveRepo
    Private Sub New()
    End Sub

    Public Shared Function List(employeeID As Integer?, status As String, year As Integer?) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "att.usp_Leave_List", Db.P("@EmployeeID", employeeID), Db.P("@Status", status), Db.P("@Year", year))
        End Using
    End Function

    Public Shared Function Apply(employeeID As Integer, leaveType As String, fromDate As Date, toDate As Date, reason As String) As Integer
        Using cn As SqlConnection = Db.OpenSession()
            Return Convert.ToInt32(Db.Scalar(cn, "att.usp_Leave_Apply", Db.P("@EmployeeID", employeeID),
                Db.P("@LeaveType", leaveType), Db.P("@FromDate", fromDate), Db.P("@ToDate", toDate), Db.P("@Reason", reason)))
        End Using
    End Function

    Public Shared Sub SetStatus(leaveID As Integer, status As String, note As String)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "att.usp_Leave_SetStatus", Db.P("@LeaveID", leaveID), Db.P("@Status", status), Db.P("@Note", note))
        End Using
    End Sub

    Public Shared Sub Cancel(leaveID As Integer)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "att.usp_Leave_Cancel", Db.P("@LeaveID", leaveID))
        End Using
    End Sub

    Public Shared Function GetBalance(employeeID As Integer, asOfDate As Date?) As LeaveBalance
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "att.usp_LeaveBalance_Get", Db.P("@EmployeeID", employeeID), Db.P("@AsOfDate", asOfDate))
            Dim b As New LeaveBalance()
            If dt.Rows.Count = 0 Then Return b
            Dim r As DataRow = dt.Rows(0)
            b.CLEntitlement = Convert.ToDecimal(r("CLEntitlement"))
            b.CLTaken = Convert.ToDecimal(r("CLTaken"))
            b.CLBalance = Convert.ToDecimal(r("CLBalance"))
            b.SLEntitlement = Convert.ToDecimal(r("SLEntitlement"))
            b.SLTaken = Convert.ToDecimal(r("SLTaken"))
            b.SLBalance = Convert.ToDecimal(r("SLBalance"))
            b.ELEarned = Convert.ToDecimal(r("ELEarned"))
            b.ELTaken = Convert.ToDecimal(r("ELTaken"))
            b.ELBalance = Convert.ToDecimal(r("ELBalance"))
            b.AsOfDate = Convert.ToDateTime(r("AsOfDate"))
            Return b
        End Using
    End Function

    Public Shared Function GetPolicy() As LeavePolicy
        Using cn As SqlConnection = Db.OpenSession()
            Dim dt As DataTable = Db.Table(cn, "cfg.usp_LeavePolicy_Get")
            Dim p As New LeavePolicy()
            If dt.Rows.Count = 0 Then Return p
            Dim r As DataRow = dt.Rows(0)
            p.CLAnnualDays = Convert.ToDecimal(r("CLAnnualDays"))
            p.SLAnnualDays = Convert.ToDecimal(r("SLAnnualDays"))
            p.ELDivisorDays = Convert.ToInt32(r("ELDivisorDays"))
            Return p
        End Using
    End Function

    Public Shared Sub SavePolicy(p As LeavePolicy)
        Using cn As SqlConnection = Db.OpenSession()
            Db.Exec(cn, "cfg.usp_LeavePolicy_Save", Db.P("@CLAnnualDays", p.CLAnnualDays),
                    Db.P("@SLAnnualDays", p.SLAnnualDays), Db.P("@ELDivisorDays", p.ELDivisorDays))
        End Using
    End Sub
End Class
