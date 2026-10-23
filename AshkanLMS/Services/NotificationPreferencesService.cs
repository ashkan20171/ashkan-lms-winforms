using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
namespace AshkanLMS.Services {
 [Serializable] public sealed class NotificationPreferences { public int DaysAhead {get;set;}=7; public bool ShowFinancial {get;set;}=true; public bool ShowActivities {get;set;}=true; public bool ShowDashboardSummary {get;set;}=true; }
 public static class NotificationPreferencesService {
  static string PathForUser(){string user=AshkanLMS.Core.AppSession.UserName??"unknown";string safe=Convert.ToBase64String(Encoding.UTF8.GetBytes(user)).Replace('/','_').Replace('+','-').TrimEnd('=');return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS","notification-preferences-"+safe+".xml");}
  public static NotificationPreferences Load(){var file=PathForUser();if(!File.Exists(file))return new NotificationPreferences();try{using(var stream=File.OpenRead(file)){var item=(NotificationPreferences)new XmlSerializer(typeof(NotificationPreferences)).Deserialize(stream);return Validate(item);}}catch(InvalidOperationException){return new NotificationPreferences();}catch(IOException){return new NotificationPreferences();}}
  public static NotificationPreferences Validate(NotificationPreferences item){if(item==null)return new NotificationPreferences();item.DaysAhead=Math.Max(1,Math.Min(90,item.DaysAhead));return item;}
  public static void Save(NotificationPreferences item){item=Validate(item);var path=PathForUser();Directory.CreateDirectory(Path.GetDirectoryName(path));var temp=path+".tmp";using(var stream=File.Create(temp)){new XmlSerializer(typeof(NotificationPreferences)).Serialize(stream,item);}if(File.Exists(path))File.Delete(path);File.Move(temp,path);}
 }
}
