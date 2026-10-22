using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AshkanLMS.Models;

namespace AshkanLMS.Services {
 public static class AcademicExportService {
  static string Csv(object value) {
   var s=Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture)??"";
   var trimmed=s.TrimStart(' ', '\t', '\r', '\n');
   if(trimmed.Length>0 && "=+-@".IndexOf(trimmed[0])>=0) s="'"+s;
   return "\""+s.Replace("\"","\"\"")+"\"";
  }
  public static void ExportGrid(DataGridView grid,string fileName) {
   if(grid==null)throw new ArgumentNullException("grid");
   using(var writer=new StreamWriter(fileName,false,new UTF8Encoding(true))) {
    var columns=new System.Collections.Generic.List<DataGridViewColumn>();
    foreach(DataGridViewColumn col in grid.Columns)if(col.Visible)columns.Add(col);
    writer.WriteLine(string.Join(",",columns.ConvertAll(c=>Csv(c.HeaderText))));
    foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow)continue;
     writer.WriteLine(string.Join(",",columns.ConvertAll(c=>Csv(row.Cells[c.Index].Value))));}
   }
  }
  // SpreadsheetML 2003 is an Excel-readable workbook without third-party packages.
  // All cells are explicitly strings, so imported user text cannot become formulas.
  public static void ExportExcelXml(DataGridView grid,string fileName) {
   if(grid==null)throw new ArgumentNullException("grid");
   var settings=new System.Xml.XmlWriterSettings { Encoding=new UTF8Encoding(true), Indent=true };
   using(var xml=System.Xml.XmlWriter.Create(fileName,settings)) {
    const string ns="urn:schemas-microsoft-com:office:spreadsheet";
    xml.WriteStartDocument(); xml.WriteStartElement("Workbook",ns);
    xml.WriteAttributeString("xmlns","ss",null,ns);
    xml.WriteStartElement("Worksheet",ns);xml.WriteAttributeString("ss","Name",ns,"LMS Report");
    xml.WriteStartElement("Table",ns);
    var columns=new System.Collections.Generic.List<DataGridViewColumn>();
    foreach(DataGridViewColumn c in grid.Columns)if(c.Visible)columns.Add(c);
    Action<System.Collections.Generic.IEnumerable<string>> row=values=>{
     xml.WriteStartElement("Row",ns);
     foreach(var v in values){xml.WriteStartElement("Cell",ns);xml.WriteStartElement("Data",ns);
      xml.WriteAttributeString("ss","Type",ns,"String");xml.WriteString(v??"");
      xml.WriteEndElement();xml.WriteEndElement();}
     xml.WriteEndElement();};
    row(columns.ConvertAll(c=>c.HeaderText));
    foreach(DataGridViewRow r in grid.Rows)if(!r.IsNewRow)
     row(columns.ConvertAll(c=>Convert.ToString(r.Cells[c.Index].Value,System.Globalization.CultureInfo.InvariantCulture)??""));
    xml.WriteEndElement();xml.WriteEndElement();xml.WriteEndElement();xml.WriteEndDocument();
   }
  }
  static string Html(string value){return System.Net.WebUtility.HtmlEncode(value??"");}
  public static void ExportCertificate(Certificate certificate,string fileName){
   if(certificate==null)throw new ArgumentNullException("certificate");
   var html="<!doctype html><html lang='en'><head><meta charset='utf-8'><title>AshkanLMS Certificate</title>"+
    "<style>@page{size:A4 landscape;margin:18mm}body{font-family:Segoe UI,Tahoma,Arial,sans-serif;background:#f5f7fb;color:#19314b;text-align:center;padding:40px}"+
    ".paper{background:white;border:9px double #b58c3b;padding:55px 35px;min-height:370px}h1{color:#15375a;font-size:38px;letter-spacing:2px}"+
    ".name{font-size:30px;font-weight:bold;margin:25px}.course{font-size:24px;color:#1b7181}.code{font-size:13px;margin-top:50px;color:#526274}"+
    "@media print{body{background:white;padding:0}.paper{min-height:0}}</style></head><body><div class='paper'>"+
    "<h1>ASHKAN LMS</h1><h2>Certificate of Completion</h2><p>This certifies that</p><div class='name'>"+Html(certificate.Student)+
    "</div><p>has successfully completed</p><div class='course'>"+Html(certificate.Course)+"</div><div class='code'>Issued: "+
    Html(certificate.IssuedAt.ToString("yyyy-MM-dd"))+" &nbsp; | &nbsp; Verification code: "+Html(certificate.Code)+
    "</div></div></body></html>";
   File.WriteAllText(fileName,html,new UTF8Encoding(false));
  }
 }
}
