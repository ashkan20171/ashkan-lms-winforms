using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
namespace AshkanLMS.Services {
 [Serializable]
 public class InvoiceBranding {
  public string AcademyName {get;set;}
  public string Address {get;set;}
  public string Phone {get;set;}
  public string Website {get;set;}
  public string LogoPath {get;set;}
  public string Footer {get;set;}
  public InvoiceBranding(){AcademyName="Ashkan Academy";Address="";Phone="";Website="";LogoPath="";Footer="Thank you for your payment";}
 }
 public static class InvoiceBrandingService {
  private static string DirectoryPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS");}}
  private static string SettingsPath {get{return Path.Combine(DirectoryPath,"invoice-branding.xml");}}
  public static InvoiceBranding Load(){
   try {if(!File.Exists(SettingsPath))return new InvoiceBranding();using(var stream=File.OpenRead(SettingsPath))return (InvoiceBranding)new XmlSerializer(typeof(InvoiceBranding)).Deserialize(stream);}
   catch(InvalidOperationException){return new InvoiceBranding();}
   catch(IOException){return new InvoiceBranding();}
   catch(UnauthorizedAccessException){return new InvoiceBranding();}
  }
  public static void Save(InvoiceBranding settings){
   if(settings==null)throw new ArgumentNullException("settings");
   if(string.IsNullOrWhiteSpace(settings.AcademyName))throw new ArgumentException("Academy name is required");
   if(settings.AcademyName.Length>120||settings.Address.Length>240||settings.Phone.Length>60||settings.Website.Length>120||settings.Footer.Length>240)throw new ArgumentException("Branding text is too long");
   Directory.CreateDirectory(DirectoryPath);
   var temp=SettingsPath+".tmp";
   using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))new XmlSerializer(typeof(InvoiceBranding)).Serialize(stream,settings);
   if(File.Exists(SettingsPath))File.Replace(temp,SettingsPath,null);else File.Move(temp,SettingsPath);
  }
  public static string Sanitize(string value){return (value??"").Replace("\r"," ").Replace("\n"," ").Trim();}
 }
}
