using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class InstallmentReminder {
  public int InvoiceId{get;set;} public int StudentId{get;set;} public int Sequence{get;set;}
  public string Currency{get;set;} public DateTime DueDate{get;set;} public decimal Outstanding{get;set;}
  public string Category{get;set;} public string Key{get{return InvoiceId.ToString(CultureInfo.InvariantCulture)+":"+Sequence.ToString(CultureInfo.InvariantCulture);}}
 }
 public static class InstallmentReminderService {
  // Reminders are informational only: no message is sent without a separate delivery integration.
  public static List<InstallmentReminder> GetPending(IEnumerable<TuitionInvoice> invoices,IEnumerable<TuitionInstallment> installments,IEnumerable<TuitionPayment> payments,DateTime today,int daysAhead){
   if(daysAhead<0||daysAhead>90)throw new ArgumentOutOfRangeException("daysAhead");
   return InstallmentReportingService.Build(invoices,installments,payments,today)
    .Where(x=>x.Outstanding>0m && x.DueDate.Date<=today.Date.AddDays(daysAhead))
    .Select(x=>new InstallmentReminder{InvoiceId=x.InvoiceId,StudentId=x.StudentId,Sequence=x.Sequence,Currency=x.Currency,DueDate=x.DueDate,Outstanding=x.Outstanding,Category=x.DueDate.Date<today.Date?"Overdue":x.DueDate.Date==today.Date?"Due today":"Upcoming"})
    .OrderBy(x=>x.DueDate).ThenBy(x=>x.InvoiceId).ThenBy(x=>x.Sequence).ToList();
  }
  public static string Draft(InstallmentReminder item,bool persian){
   if(item==null)throw new ArgumentNullException("item");
   return persian?string.Format(CultureInfo.InvariantCulture,"یادآوری شهریه\r\nشناسه دانشجو: {0}\r\nصورتحساب: {1}، قسط: {2}\r\nسررسید: {3:yyyy-MM-dd}\r\nمانده: {4:N2} {5}\r\nلطفاً وضعیت پرداخت را بررسی کنید.",item.StudentId,item.InvoiceId,item.Sequence,item.DueDate,item.Outstanding,item.Currency):string.Format(CultureInfo.InvariantCulture,"Tuition payment reminder\r\nStudent ID: {0}\r\nInvoice: {1}, installment: {2}\r\nDue: {3:yyyy-MM-dd}\r\nOutstanding: {4:N2} {5}\r\nPlease review the payment status.",item.StudentId,item.InvoiceId,item.Sequence,item.DueDate,item.Outstanding,item.Currency);
  }
  public static string Csv(IEnumerable<InstallmentReminder> reminders){
   var b=new StringBuilder("InvoiceId,StudentId,Installment,DueDate,Outstanding,Currency,Category\r\n");
   foreach(var x in reminders)b.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3:yyyy-MM-dd},{4},{5},{6}\r\n",x.InvoiceId,x.StudentId,x.Sequence,x.DueDate,x.Outstanding,x.Currency,x.Category);
   return "\uFEFF"+b.ToString();
  }
 }
 // Local operator acknowledgement history; this is not proof that a student was contacted.
 public static class ReminderAcknowledgementStore {
  private static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS");
  private static readonly string FilePath=Path.Combine(Folder,"reminder-acknowledgements.tsv");
  public static HashSet<string> Read(){
   if(!File.Exists(FilePath))return new HashSet<string>(StringComparer.Ordinal);
   return new HashSet<string>(File.ReadAllLines(FilePath,Encoding.UTF8).Where(x=>x.Length>0),StringComparer.Ordinal);
  }
  public static void Mark(string key){
   if(string.IsNullOrWhiteSpace(key)||!System.Text.RegularExpressions.Regex.IsMatch(key,@"^\d+:\d+$"))throw new ArgumentException("Invalid reminder key");
   Directory.CreateDirectory(Folder);var existing=Read();if(existing.Add(key))File.AppendAllText(FilePath,key+Environment.NewLine,Encoding.UTF8);
  }
 }
}
