Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports System.Text
Imports System.Xml.Linq

''' <summary>Reads .xlsx (first sheet) and .csv into rows of text, writes .csv. No Excel or add-on needed:
''' .xlsx is a zip of XML files. Blank rows are kept so "Row N" matches what the user sees in Excel.</summary>
Public NotInheritable Class TabularFile
    Private Shared ReadOnly NS_MAIN As XNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    Private Shared ReadOnly NS_REL As XNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
    Private Shared ReadOnly NS_PKG_REL As XNamespace = "http://schemas.openxmlformats.org/package/2006/relationships"

    Private Shared ReadOnly CR As Char = Convert.ToChar(13)
    Private Shared ReadOnly LF As Char = Convert.ToChar(10)
    Private Shared ReadOnly TAB As Char = Convert.ToChar(9)
    Private Shared ReadOnly CRLF As String = Convert.ToChar(13).ToString() & Convert.ToChar(10).ToString()

    Private Sub New()
    End Sub

    Public Shared Function Read(path As String) As List(Of String())
        Select Case System.IO.Path.GetExtension(path).ToLowerInvariant()
            Case ".csv", ".txt"
                Return ReadCsv(File.ReadAllText(path, Encoding.UTF8))
            Case ".xlsx"
                Using fs As FileStream = File.OpenRead(path)
                    Return ReadXlsx(fs)
                End Using
            Case ".xls"
                Throw New BusinessException("Old .xls files cannot be read. Save the sheet as .xlsx or .csv and try again.")
            Case Else
                Throw New BusinessException("Choose an .xlsx or .csv file.")
        End Select
    End Function

    ' ---------- CSV ----------
    Public Shared Function ReadCsv(text As String) As List(Of String())
        Dim rows As New List(Of String())()
        If String.IsNullOrEmpty(text) Then Return rows
        Dim delim As Char = DetectDelimiter(text)
        Dim fields As New List(Of String)()
        Dim sb As New StringBuilder()
        Dim inQuotes As Boolean = False
        Dim i As Integer = 0
        Do While i < text.Length
            Dim ch As Char = text(i)
            If inQuotes Then
                If ch = """"c Then
                    If i + 1 < text.Length AndAlso text(i + 1) = """"c Then
                        sb.Append(""""c)
                        i += 1
                    Else
                        inQuotes = False
                    End If
                Else
                    sb.Append(ch)
                End If
            ElseIf ch = """"c Then
                inQuotes = True
            ElseIf ch = delim Then
                fields.Add(sb.ToString().Trim())
                sb.Clear()
            ElseIf ch = CR OrElse ch = LF Then
                If ch = CR AndAlso i + 1 < text.Length AndAlso text(i + 1) = LF Then i += 1
                fields.Add(sb.ToString().Trim())
                sb.Clear()
                rows.Add(fields.ToArray())
                fields.Clear()
            Else
                sb.Append(ch)
            End If
            i += 1
        Loop
        If sb.Length > 0 OrElse fields.Count > 0 Then
            fields.Add(sb.ToString().Trim())
            rows.Add(fields.ToArray())
        End If
        Return rows
    End Function

    Private Shared Function DetectDelimiter(text As String) As Char
        Dim firstLine As String = text
        Dim nl As Integer = text.IndexOfAny(New Char() {CR, LF})
        If nl >= 0 Then firstLine = text.Substring(0, nl)
        Dim best As Char = ","c
        Dim bestCount As Integer = -1
        For Each c As Char In New Char() {","c, ";"c, TAB}
            Dim n As Integer = 0
            For Each ch As Char In firstLine
                If ch = c Then n += 1
            Next
            If n > bestCount Then
                best = c
                bestCount = n
            End If
        Next
        Return best
    End Function

    Public Shared Sub WriteCsv(path As String, rows As IEnumerable(Of String()))
        Dim sb As New StringBuilder()
        For Each row As String() In rows
            For i As Integer = 0 To row.Length - 1
                If i > 0 Then sb.Append(","c)
                sb.Append(Quote(row(i)))
            Next
            sb.Append(CRLF)
        Next
        File.WriteAllText(path, sb.ToString(), New UTF8Encoding(True))   ' BOM so Excel shows Unicode correctly
    End Sub

    Private Shared Function Quote(value As String) As String
        If value Is Nothing Then Return String.Empty
        If value.IndexOfAny(New Char() {","c, """"c, CR, LF}) < 0 Then Return value
        Return """" & value.Replace("""", """""") & """"
    End Function

    ' ---------- XLSX ----------
    Public Shared Function ReadXlsx(stream As Stream) As List(Of String())
        Try
            Using zip As New ZipArchive(stream, ZipArchiveMode.Read, True)
                Dim shared_ As List(Of String) = ReadSharedStrings(zip)
                Dim sheetEntry As ZipArchiveEntry = FindFirstSheet(zip)
                If sheetEntry Is Nothing Then Throw New BusinessException("No worksheet found in the file.")
                Using s As Stream = sheetEntry.Open()
                    Return ReadSheet(XDocument.Load(s), shared_)
                End Using
            End Using
        Catch ex As InvalidDataException
            Throw New BusinessException("The file is not a valid .xlsx workbook.", ex)
        Catch ex As System.Xml.XmlException
            Throw New BusinessException("The worksheet could not be read.", ex)
        End Try
    End Function

    Private Shared Function ReadSharedStrings(zip As ZipArchive) As List(Of String)
        Dim result As New List(Of String)()
        Dim entry As ZipArchiveEntry = zip.GetEntry("xl/sharedStrings.xml")
        If entry Is Nothing Then Return result
        Using s As Stream = entry.Open()
            Dim doc As XDocument = XDocument.Load(s)
            For Each si As XElement In doc.Descendants(NS_MAIN + "si")
                Dim sb As New StringBuilder()
                For Each t As XElement In si.Descendants(NS_MAIN + "t")
                    sb.Append(t.Value)
                Next
                result.Add(sb.ToString())
            Next
        End Using
        Return result
    End Function

    Private Shared Function FindFirstSheet(zip As ZipArchive) As ZipArchiveEntry
        Dim wb As ZipArchiveEntry = zip.GetEntry("xl/workbook.xml")
        Dim rels As ZipArchiveEntry = zip.GetEntry("xl/_rels/workbook.xml.rels")
        If wb IsNot Nothing AndAlso rels IsNot Nothing Then
            Dim relId As String = Nothing
            Using s As Stream = wb.Open()
                Dim sheet As XElement = XDocument.Load(s).Descendants(NS_MAIN + "sheet").FirstOrDefault()
                If sheet IsNot Nothing Then relId = CStr(sheet.Attribute(NS_REL + "id"))
            End Using
            If relId IsNot Nothing Then
                Using s As Stream = rels.Open()
                    For Each r As XElement In XDocument.Load(s).Descendants(NS_PKG_REL + "Relationship")
                        If CStr(r.Attribute("Id")) = relId Then
                            Dim target As String = CStr(r.Attribute("Target")).TrimStart("/"c)
                            If Not target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) Then target = "xl/" & target
                            Dim found As ZipArchiveEntry = zip.GetEntry(target)
                            If found IsNot Nothing Then Return found
                        End If
                    Next
                End Using
            End If
        End If
        Return zip.GetEntry("xl/worksheets/sheet1.xml")
    End Function

    Private Shared Function ReadSheet(doc As XDocument, sharedStrings As List(Of String)) As List(Of String())
        Dim rows As New List(Of String())()
        For Each rowEl As XElement In doc.Descendants(NS_MAIN + "row")
            Dim rowNo As Integer = rows.Count + 1
            Dim attr As XAttribute = rowEl.Attribute("r")
            If attr IsNot Nothing Then Integer.TryParse(attr.Value, rowNo)
            Do While rows.Count < rowNo - 1
                rows.Add(New String() {})
            Loop
            Dim cells As New List(Of String)()
            For Each c As XElement In rowEl.Elements(NS_MAIN + "c")
                Dim col As Integer = cells.Count
                Dim refAttr As XAttribute = c.Attribute("r")
                If refAttr IsNot Nothing Then col = ColumnIndex(refAttr.Value)
                Do While cells.Count < col
                    cells.Add(String.Empty)
                Loop
                cells.Add(CellText(c, sharedStrings))
            Next
            rows.Add(cells.ToArray())
        Next
        Return rows
    End Function

    Private Shared Function CellText(c As XElement, sharedStrings As List(Of String)) As String
        Dim t As String = CStr(c.Attribute("t"))
        Dim v As XElement = c.Element(NS_MAIN + "v")
        Select Case t
            Case "s"
                Dim idx As Integer
                If v IsNot Nothing AndAlso Integer.TryParse(v.Value, idx) AndAlso idx >= 0 AndAlso idx < sharedStrings.Count Then Return sharedStrings(idx).Trim()
                Return String.Empty
            Case "inlineStr"
                Dim sb As New StringBuilder()
                For Each tt As XElement In c.Descendants(NS_MAIN + "t")
                    sb.Append(tt.Value)
                Next
                Return sb.ToString().Trim()
            Case "b"
                Return If(v IsNot Nothing AndAlso v.Value = "1", "1", "0")
            Case Else
                Return If(v Is Nothing, String.Empty, v.Value.Trim())
        End Select
    End Function

    ''' <summary>"A" -> 0, "B" -> 1, "AA" -> 26. Reads only the letters of a reference like "AB12".</summary>
    Friend Shared Function ColumnIndex(cellRef As String) As Integer
        Dim n As Integer = 0
        For Each ch As Char In cellRef
            If ch >= "A"c AndAlso ch <= "Z"c Then
                n = n * 26 + (Convert.ToInt32(ch) - Convert.ToInt32("A"c) + 1)
            ElseIf ch >= "a"c AndAlso ch <= "z"c Then
                n = n * 26 + (Convert.ToInt32(ch) - Convert.ToInt32("a"c) + 1)
            Else
                Exit For
            End If
        Next
        Return Math.Max(0, n - 1)
    End Function
End Class
