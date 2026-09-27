Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms
Imports HRMS.Common

''' <summary>The whole application menu (design.md 2.x) with the permission each item needs.
''' Only items the user may use are shown; a group with no visible items is hidden.</summary>
Friend NotInheritable Class MenuCatalog
    Private Sub New()
    End Sub

    Public Shared Sub AppendModules(menu As MenuStrip, session As UserSession, onClick As Action(Of MenuEntry))
        For Each group As KeyValuePair(Of String, List(Of MenuEntry)) In Groups()
            Dim top As New ToolStripMenuItem(group.Key)
            For Each entry As MenuEntry In group.Value
                If entry Is Nothing Then
                    If top.DropDownItems.Count > 0 Then top.DropDownItems.Add(New ToolStripSeparator())
                ElseIf Allowed(session, entry.Permission) Then
                    Dim item As New ToolStripMenuItem(entry.Caption)
                    Dim captured As MenuEntry = entry
                    AddHandler item.Click, Sub(s As Object, e As EventArgs) onClick(captured)
                    top.DropDownItems.Add(item)
                End If
            Next
            If top.DropDownItems.Count > 0 Then menu.Items.Add(top)
        Next
    End Sub

    Private Shared Function Allowed(session As UserSession, permission As String) As Boolean
        If String.IsNullOrEmpty(permission) Then Return True
        For Each code As String In permission.Split("|"c)
            If session.Has(code) Then Return True
        Next
        Return False
    End Function

    Private Shared Function Groups() As List(Of KeyValuePair(Of String, List(Of MenuEntry)))
        Dim g As New List(Of KeyValuePair(Of String, List(Of MenuEntry)))()
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("&Masters", New List(Of MenuEntry) From {
            New MenuEntry("mst.company", "Company", "COMPANY_VIEW", "Phase 4"),
            New MenuEntry("mst.division", "Division", "COMPANY_VIEW", "Phase 4"),
            New MenuEntry("mst.factory", "Factory / site", "COMPANY_VIEW", "Phase 4"),
            New MenuEntry("mst.dept", "Department", "EMPLOYEE_VIEW", "Phase 4"),
            New MenuEntry("mst.desig", "Designation", "EMPLOYEE_VIEW", "Phase 4"),
            New MenuEntry("mst.employee", "Employee", "EMPLOYEE_VIEW", "Phase 4"),
            Nothing,
            New MenuEntry("mst.minwage", "Minimum wages", "COMPANY_VIEW", "Phase 4"),
            New MenuEntry("mst.holiday", "Holiday calendar", "ATT_VIEW", "Phase 4"),
            New MenuEntry("mst.fy", "Financial year", "COMPANY_EDIT", "Phase 4")}))
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("&Attendance", New List(Of MenuEntry) From {
            New MenuEntry("att.monthly", "Monthly attendance", "ATT_VIEW", "Phase 5"),
            Nothing,
            New MenuEntry("att.daily", "Daily attendance (optional mode)", "ATT_VIEW", "Phase 5b"),
            New MenuEntry("att.leave", "Leave register", "ATT_EDIT", "Phase 5b"),
            New MenuEntry("att.shift", "Shifts", "ATT_EDIT", "Phase 5b")}))
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("&Payroll", New List(Of MenuEntry) From {
            New MenuEntry("pay.struct", "Salary structure", "PAY_VIEW", "Phase 6"),
            New MenuEntry("pay.loan", "Advance / loan", "PAY_PROCESS", "Phase 6"),
            New MenuEntry("pay.arrears", "Arrears and bonus", "PAY_PROCESS", "Phase 6"),
            Nothing,
            New MenuEntry("pay.process", "Process payroll", "PAY_PROCESS", "Phase 6"),
            New MenuEntry("pay.approve", "Approve payroll", "PAY_APPROVE", "Phase 6"),
            New MenuEntry("pay.payslip", "Payslip", "PAY_VIEW|PAYSLIP_SELF", "Phase 6")}))
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("&Statutory", New List(Of MenuEntry) From {
            New MenuEntry("stat.setup", "ESI / EPF setup", "STAT_VIEW", "Phase 7"),
            New MenuEntry("stat.rates", "Rate table", "RATE_EDIT", "Phase 7"),
            Nothing,
            New MenuEntry("stat.ecr", "EPF ECR export", "STAT_EXPORT", "Phase 7"),
            New MenuEntry("stat.esi", "ESI contribution export", "STAT_EXPORT", "Phase 7"),
            Nothing,
            New MenuEntry("stat.gratuity", "Gratuity", "GRATUITY_EDIT", "Phase 7"),
            New MenuEntry("stat.bonus", "Bonus", "GRATUITY_EDIT", "Phase 7")}))
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("Care&er", New List(Of MenuEntry) From {
            New MenuEntry("car.promotion", "Promotion", "CAREER_EDIT", "Phase 8"),
            New MenuEntry("car.increment", "Increment", "CAREER_EDIT", "Phase 8"),
            New MenuEntry("car.transfer", "Transfer", "CAREER_EDIT", "Phase 8"),
            New MenuEntry("car.history", "Career history", "CAREER_EDIT", "Phase 8")}))
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("&Reports", New List(Of MenuEntry) From {
            New MenuEntry("rep.centre", "Report centre", "REPORT_VIEW", "Phase 9")}))
        g.Add(New KeyValuePair(Of String, List(Of MenuEntry))("A&dmin", New List(Of MenuEntry) From {
            New MenuEntry("adm.users", "Users", "USER_ADMIN", "Phase 10"),
            New MenuEntry("adm.audit", "Audit log", "AUDIT_VIEW", "Phase 10"),
            Nothing,
            New MenuEntry("adm.backup", "Backup", "BACKUP_ADMIN", "Phase 10"),
            New MenuEntry("adm.restore", "Restore", "BACKUP_ADMIN", "Phase 10"),
            New MenuEntry("adm.license", "License", "LICENSE_ADMIN", "Phase 11"),
            New MenuEntry("adm.settings", "Settings", "USER_ADMIN", "Phase 10")}))
        Return g
    End Function
End Class
