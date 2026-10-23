using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using AshkanLMS.Core;
namespace AshkanLMS.Services {
 public sealed class ReminderEmailRequest {
  public string Host, Username, Password, Sender, Recipient, Subject, Body;
  public int Port; public bool EnableSsl;
 }
 public static class ReminderEmailService {
  private static readonly string HistoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS","reminder-email-history.tsv");
  public static void Validate(ReminderEmailRequest r){
   if(r==null)throw new ArgumentNullException("r");
   if(string.IsNullOrWhiteSpace(r.Host)||r.Host.Length>255||r.Host.Contains(" "))throw new ArgumentException("Invalid SMTP host");
   if(r.Port<1||r.Port>65535)throw new ArgumentException("Invalid SMTP port");
   if(!r.EnableSsl)throw new ArgumentException("TLS must be enabled for SMTP delivery");
   if(string.IsNullOrWhiteSpace(r.Sender)||string.IsNullOrWhiteSpace(r.Recipient))throw new ArgumentException("Sender and recipient required");
   {var a=new MailAddress(r.Sender);var b=new MailAddress(r.Recipient);
    if(a.Address!=r.Sender.Trim()||b.Address!=r.Recipient.Trim())throw new ArgumentException("Single email addresses required");
   }
   if(string.IsNullOrWhiteSpace(r.Subject)||string.IsNullOrWhiteSpace(r.Body))throw new ArgumentException("Subject and message required");
  }
  public static void Send(ReminderEmailRequest r){
   if(AppSession.Role!="Administrator")throw new UnauthorizedAccessException("Administrator access required");
   Validate(r);
   using(var message=new MailMessage(new MailAddress(r.Sender),new MailAddress(r.Recipient))){
    message.Subject=r.Subject;message.Body=r.Body;message.SubjectEncoding=Encoding.UTF8;message.BodyEncoding=Encoding.UTF8;message.IsBodyHtml=false;
    using(var smtp=new SmtpClient(r.Host,r.Port)){
     smtp.EnableSsl=true;smtp.DeliveryMethod=SmtpDeliveryMethod.Network;smtp.UseDefaultCredentials=false;
     smtp.Credentials=new NetworkCredential(r.Username??"",r.Password??"");smtp.Timeout=20000;
     smtp.Send(message);
    }
   }
  }
  // Append only after successful SMTP handoff; this is NOT proof of delivery to inbox.
  public static void RecordAccepted(string key,string recipient){
   if(!Regex.IsMatch(key??"",@"^\d+:\d+$"))throw new ArgumentException("Invalid reminder key");
   if(string.IsNullOrWhiteSpace(recipient))throw new ArgumentException("Invalid recipient");
   var directory=Path.GetDirectoryName(HistoryPath);Directory.CreateDirectory(directory);
   var safeRecipient=recipient.Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
   File.AppendAllText(HistoryPath,DateTimeOffset.Now.ToString("o")+"\t"+key+"\t"+safeRecipient+Environment.NewLine,Encoding.UTF8);
  }
  public static bool WasAccepted(string key,string recipient){
   return ReadHistory().Any(line=>{var parts=line.Split('\t');return parts.Length>=3 && parts[1]==key && string.Equals(parts[2],recipient,StringComparison.OrdinalIgnoreCase);});
  }
  public static void RecordFailure(string key,string recipient,string reason){
   if(!Regex.IsMatch(key??"",@"^\d+:\d+$"))throw new ArgumentException("Invalid reminder key");
   var directory=Path.GetDirectoryName(FailurePath);Directory.CreateDirectory(directory);
   var safeRecipient=(recipient??"").Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
   var safeReason=(reason??"").Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
   File.AppendAllText(FailurePath,DateTimeOffset.Now.ToString("o")+"\t"+key+"\t"+safeRecipient+"\t"+safeReason+Environment.NewLine,Encoding.UTF8);
  }
  public static string[] ReadFailures(){return File.Exists(FailurePath)?File.ReadAllLines(FailurePath,Encoding.UTF8):new string[0];}
  private static readonly string FailurePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS","reminder-email-failures.tsv");
  public static string[] ReadHistory(){return File.Exists(HistoryPath)?File.ReadAllLines(HistoryPath,Encoding.UTF8):new string[0];}
 }
}
