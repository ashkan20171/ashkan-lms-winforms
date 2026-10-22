using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace AshkanLMS.Services
{
    // Minimal Office Open XML writer: no NuGet packages or Office installation required.
    internal static class AuditExcelExporter
    {
        internal static void Export(string path, DataSet data, string[] labels)
        {
            if (data == null || data.Tables.Count == 0) throw new ArgumentException("No report data.");
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Write(archive, "[Content_Types].xml", ContentTypes(data.Tables.Count));
                Write(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                var sheets = new StringBuilder();
                var rels = new StringBuilder();
                for (int i = 0; i < data.Tables.Count; i++)
                {
                    string name = SafeSheetName(labels != null && i < labels.Length ? labels[i] : data.Tables[i].TableName, i);
                    sheets.Append("<sheet name=\"").Append(Escape(name)).Append("\" sheetId=\"").Append(i + 1).Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
                    rels.Append("<Relationship Id=\"rId").Append(i + 1).Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet").Append(i + 1).Append(".xml\"/>");
                    Write(archive, "xl/worksheets/sheet" + (i + 1) + ".xml", Sheet(data.Tables[i]));
                }
                Write(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" + sheets + "</sheets></workbook>");
                Write(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" + rels + "</Relationships>");
            }
        }
        private static string Sheet(DataTable table)
        {
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            xml.Append("<row r=\"1\">");
            for (int c = 0; c < table.Columns.Count; c++) Cell(xml, c, 1, table.Columns[c].ColumnName, false);
            xml.Append("</row>");
            for (int r = 0; r < table.Rows.Count; r++)
            {
                xml.Append("<row r=\"").Append(r + 2).Append("\">");
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    object value = table.Rows[r][c];
                    bool numeric = value != DBNull.Value && (value is byte || value is short || value is int || value is long || value is float || value is double || value is decimal);
                    string text = value == DBNull.Value ? "" : numeric ? Convert.ToString(value, CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
                    Cell(xml, c, r + 2, text, numeric);
                }
                xml.Append("</row>");
            }
            return xml.Append("</sheetData></worksheet>").ToString();
        }
        private static void Cell(StringBuilder xml, int col, int row, string value, bool numeric)
        {
            string column = "";
            for (int n = col + 1; n > 0; n = (n - 1) / 26) column = (char)('A' + (n - 1) % 26) + column;
            xml.Append("<c r=\"").Append(column).Append(row).Append("\"");
            if (numeric) xml.Append("><v>").Append(value).Append("</v></c>");
            else
            {
                // Prevent spreadsheet formula injection in exported text.
                if (!string.IsNullOrEmpty(value) && "=+-@".IndexOf(value.TrimStart().Length > 0 ? value.TrimStart()[0] : '\0') >= 0) value = "'" + value;
                xml.Append(" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Escape(value)).Append("</t></is></c>");
            }
        }
        private static string Escape(string value) { return SecurityElement.Escape(value ?? "") ?? ""; }
        private static string SafeSheetName(string value, int index)
        {
            if (string.IsNullOrWhiteSpace(value)) value = "Sheet" + (index + 1);
            foreach (char c in "[]:*?/\\") value = value.Replace(c, '_');
            return value.Length > 31 ? value.Substring(0, 31) : value;
        }
        private static string ContentTypes(int count)
        {
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            for (int i = 1; i <= count; i++) xml.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i).Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            return xml.Append("</Types>").ToString();
        }
        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }
    }
}
