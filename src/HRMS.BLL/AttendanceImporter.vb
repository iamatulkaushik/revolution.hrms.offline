Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text
Imports HRMS.Models

''' <summary>Turns rows read from an .xlsx/.csv file into attendance entries. Nothing is saved here.
''' Recognised headers (any case, spaces ignored): Code, WD, HD, CL, EL, SL, Comp, OT, Remarks.</summary>
Public NotInheritable Class AttendanceImporter
    Private Const HEADER_SEARCH_ROWS As Integer = 10

    Private Shared ReadOnly CODE_ALIASES As String() = {"code", "empcode", "employeecode", "ecode", "empno", "employeeno", "employeeid"}
    Private Shared ReadOnly WORKING_ALIASES As String() = {"wd", "workingdays", "daysworked", "present", "presentdays", "days"}
    Private Shared ReadOnly HOLIDAY_ALIASES As String() = {"hd", "holidays", "holiday", "weeklyoff", "wo", "weeklyoffs"}
    Private Shared ReadOnly CL_ALIASES As String() = {"cl", "casualleave", "casual"}
    Private Shared ReadOnly EL_ALIASES As String() = {"el", "earnedleave", "earned", "privilegeleave", "pl"}
    Private Shared ReadOnly SL_ALIASES As String() = {"sl", "sickleave", "sick"}
    Private Shared ReadOnly COMP_ALIASES As String() = {"comp", "compleave", "compoff", "compensatoryleave"}
    Private Shared ReadOnly OT_ALIASES As String() = {"ot", "othours", "overtime", "overtimehours"}
    Private Shared ReadOnly REMARK_ALIASES As String() = {"remarks", "remark", "note", "notes"}

    Private Sub New()
    End Sub

    ''' <param name="codeToId">Employee code (case-insensitive) to EmployeeID for the employees shown on screen.</param>
    Public Shared Function Parse(rows As List(Of String()), codeToId As IDictionary(Of String, Integer),
                                 year As Integer, month As Integer) As ImportResult
        Dim result As New ImportResult()
        Dim lookup As New Dictionary(Of String, Integer)(codeToId, StringComparer.OrdinalIgnoreCase)

        Dim headerRow As Integer = FindHeaderRow(rows)
        If headerRow < 0 Then
            result.Errors.Add("Could not find a header row with an 'Employee code' column in the first " & HEADER_SEARCH_ROWS & " rows.")
            Return result
        End If

        Dim header As String() = rows(headerRow)
        Dim cCode As Integer = FindColumn(header, CODE_ALIASES)
        Dim cWd As Integer = FindColumn(header, WORKING_ALIASES)
        Dim cHd As Integer = FindColumn(header, HOLIDAY_ALIASES)
        Dim cCl As Integer = FindColumn(header, CL_ALIASES)
        Dim cEl As Integer = FindColumn(header, EL_ALIASES)
        Dim cSl As Integer = FindColumn(header, SL_ALIASES)
        Dim cComp As Integer = FindColumn(header, COMP_ALIASES)
        Dim cOt As Integer = FindColumn(header, OT_ALIASES)
        Dim cRemark As Integer = FindColumn(header, REMARK_ALIASES)
        If cWd < 0 AndAlso cHd < 0 AndAlso cCl < 0 AndAlso cEl < 0 AndAlso cSl < 0 AndAlso cComp < 0 AndAlso cOt < 0 Then
            result.Errors.Add("No attendance columns found. Expected WD, HD, CL, EL, SL, Comp or OT.")
            Return result
        End If

        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For i As Integer = headerRow + 1 To rows.Count - 1
            Dim r As String() = rows(i)
            Dim code As String = Cell(r, cCode)
            If code.Length = 0 Then Continue For
            result.RowsRead += 1
            Dim rowNo As Integer = i + 1

            Dim id As Integer
            If Not lookup.TryGetValue(code, id) Then
                result.Errors.Add("Row " & rowNo & ": employee code '" & code & "' is not in this month's list.")
                Continue For
            End If
            If Not seen.Add(code) Then
                result.Errors.Add("Row " & rowNo & ": employee code '" & code & "' appears more than once.")
                Continue For
            End If

            Dim e As New AttendanceEntry With {.EmployeeID = id, .EmpCode = code}
            Dim rowOk As Boolean = True
            e.WorkingDays = ReadNumber(result, r, cWd, rowNo, "WD", rowOk)
            e.Holidays = ReadNumber(result, r, cHd, rowNo, "HD", rowOk)
            e.CasualLeave = ReadNumber(result, r, cCl, rowNo, "CL", rowOk)
            e.EarnedLeave = ReadNumber(result, r, cEl, rowNo, "EL", rowOk)
            e.SickLeave = ReadNumber(result, r, cSl, rowNo, "SL", rowOk)
            e.CompLeave = ReadNumber(result, r, cComp, rowNo, "Comp", rowOk)
            e.OTHours = ReadNumber(result, r, cOt, rowNo, "OT", rowOk)
            Dim remark As String = Cell(r, cRemark)
            e.Remarks = If(remark.Length = 0, Nothing, If(remark.Length > 200, remark.Substring(0, 200), remark))
            If Not rowOk Then Continue For

            Dim problems As List(Of String) = AttendanceEngine.Validate(e, year, month)
            If problems.Count > 0 Then
                For Each p As String In problems
                    result.Errors.Add("Row " & rowNo & ": " & p)
                Next
                Continue For
            End If
            result.Entries.Add(e)
        Next
        Return result
    End Function

    Private Shared Function ReadNumber(result As ImportResult, r As String(), col As Integer, rowNo As Integer,
                                       label As String, ByRef ok As Boolean) As Decimal
        Dim text As String = Cell(r, col)
        If text.Length = 0 Then Return 0D
        Dim v As Decimal
        If Decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, v) Then Return v
        result.Errors.Add("Row " & rowNo & ": '" & text & "' is not a number in column " & label & ".")
        ok = False
        Return 0D
    End Function

    Private Shared Function Cell(r As String(), col As Integer) As String
        If col < 0 OrElse col >= r.Length OrElse r(col) Is Nothing Then Return String.Empty
        Return r(col).Trim()
    End Function

    Private Shared Function FindHeaderRow(rows As List(Of String())) As Integer
        For i As Integer = 0 To Math.Min(rows.Count, HEADER_SEARCH_ROWS) - 1
            If FindColumn(rows(i), CODE_ALIASES) >= 0 Then Return i
        Next
        Return -1
    End Function

    Private Shared Function FindColumn(header As String(), aliases As String()) As Integer
        For c As Integer = 0 To header.Length - 1
            If Array.IndexOf(aliases, Normalise(header(c))) >= 0 Then Return c
        Next
        Return -1
    End Function

    ''' <summary>"Emp. Code" -> "empcode"</summary>
    Private Shared Function Normalise(text As String) As String
        If text Is Nothing Then Return String.Empty
        Dim sb As New StringBuilder()
        For Each ch As Char In text
            If Char.IsLetterOrDigit(ch) Then sb.Append(Char.ToLowerInvariant(ch))
        Next
        Return sb.ToString()
    End Function
End Class
