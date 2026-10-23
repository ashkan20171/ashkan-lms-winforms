using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class CenterNotification { public string Key{get;set;} public string Kind{get;set;} public string Title{get;set;} public string Detail{get;set;} public DateTime At{get;set;} public bool IsRead{get;set;} }
 public static class NotificationCenterService {
  public static List<CenterNotification> Financial(IEnumerable<TuitionInvoice> invoices,IEnumerable<TuitionInstallment> installments,IEnumerable<TuitionPayment> payments,DateTime today,int daysAhead){
   return InstallmentReminderService.GetPending(invoices,installments,payments,today,daysAhead).Select(x=>new CenterNotification{Key="installment:"+x.Key+":"+x.DueDate.ToString("yyyyMMdd"),Kind=x.Category,Title="Invoice #"+x.InvoiceId+" / Installment #"+x.Sequence,Detail="Student #"+x.StudentId+" | "+x.Outstanding.ToString("N2",System.Globalization.CultureInfo.InvariantCulture)+" "+x.Currency+" | "+x.DueDate.ToString("yyyy-MM-dd"),At=x.DueDate}).ToList();
  }
  public static int UnreadCount(IEnumerable<CenterNotification> items,ISet<string> read){return items.Count(x=>!read.Contains(x.Key));}
 }
 public static class NotificationReadStore {
  static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS");
  static readonly string FileName=Path.Combine(Folder,"notification-read-"+SafeUser()+".txt");
  static string SafeUser(){return Convert.ToBase64String(Encoding.UTF8.GetBytes(AshkanLMS.Core.AppSession.UserName??"unknown")).Replace('/','_').Replace('+','-').TrimEnd('=');}
  public static HashSet<string> Load(){return File.Exists(FileName)?new HashSet<string>(File.ReadAllLines(FileName,Encoding.UTF8),StringComparer.Ordinal):new HashSet<string>(StringComparer.Ordinal);}
  public static void Mark(IEnumerable<string> keys){var set=Load();foreach(var key in keys){if(!string.IsNullOrWhiteSpace(key)&&key.Length<160)set.Add(key);}Directory.CreateDirectory(Folder);var temp=FileName+".tmp";File.WriteAllLines(temp,set.OrderBy(x=>x),Encoding.UTF8);if(File.Exists(FileName))File.Delete(FileName);File.Move(temp,FileName);}
 }
}
