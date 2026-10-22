using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace AshkanLMS.Services {
 public sealed class SecurityAuditService {
  private static readonly object Gate = new object();
  private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS");
  private static string LogPath { get { return Path.Combine(Folder,"security-events.log"); } }
  public sealed class Entry { public DateTime UtcTime {get;set;} public string Actor {get;set;} public string Event {get;set;} public string Subject {get;set;} }
  private static string Safe(string value){return (value??"").Replace("\r"," ").Replace("\n"," ").Replace("|","/").Trim();}
  public void Record(string actor,string eventName,string subject){
   lock(Gate){Directory.CreateDirectory(Folder);File.AppendAllText(LogPath,DateTime.UtcNow.ToString("o")+"|"+Safe(actor)+"|"+Safe(eventName)+"|"+Safe(subject)+Environment.NewLine,Encoding.UTF8);}
  }
  public List<Entry> Recent(string actingRole,int limit=200){
   if(actingRole!="Administrator")throw new UnauthorizedAccessException("Administrator required.");
   lock(Gate){if(!File.Exists(LogPath))return new List<Entry>();return File.ReadAllLines(LogPath).Reverse().Take(Math.Max(1,Math.Min(1000,limit))).Select(line=>line.TrimStart('\uFEFF').Split('|')).Where(p=>p.Length==4).Select(p=>{DateTime dt;DateTime.TryParse(p[0],null,System.Globalization.DateTimeStyles.RoundtripKind,out dt);return new Entry{UtcTime=dt,Actor=p[1],Event=p[2],Subject=p[3]};}).ToList();}
  }
 }
}
