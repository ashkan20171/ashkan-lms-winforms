using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class InstallmentReportRow {
  public int InvoiceId{get;set;} public int StudentId{get;set;} public string Currency{get;set;}
  public int Sequence{get;set;} public DateTime DueDate{get;set;} public decimal Amount{get;set;}
  public decimal Paid{get;set;} public decimal Outstanding{get;set;} public string Status{get;set;}
 }
 public static class InstallmentReportingService {
  public static List<InstallmentReportRow> Build(IEnumerable<TuitionInvoice> invoices,IEnumerable<TuitionInstallment> installments,IEnumerable<TuitionPayment> payments,DateTime asOf){
   var bills=invoices.ToDictionary(x=>x.Id);
   var pay=payments.ToList();var output=new List<InstallmentReportRow>();
   foreach(var group in installments.GroupBy(x=>x.InvoiceId)){
    TuitionInvoice invoice;if(!bills.TryGetValue(group.Key,out invoice))continue;
    foreach(var item in TuitionInstallmentService.Calculate(group,pay,asOf))output.Add(new InstallmentReportRow{InvoiceId=invoice.Id,StudentId=invoice.StudentId,Currency=invoice.Currency,Sequence=item.Sequence,DueDate=item.DueDate,Amount=item.Amount,Paid=item.Paid,Outstanding=item.Outstanding,Status=item.Status});
   }
   return output.OrderBy(x=>x.DueDate).ThenBy(x=>x.InvoiceId).ThenBy(x=>x.Sequence).ToList();
  }
  public static List<InstallmentReportRow> Filter(IEnumerable<InstallmentReportRow> rows,string currency,string status,DateTime from,DateTime until,int? studentId){
   return rows.Where(x=>(string.IsNullOrEmpty(currency)||x.Currency==currency)&&(string.IsNullOrEmpty(status)||status=="All"||x.Status==status)&&x.DueDate.Date>=from.Date&&x.DueDate.Date<=until.Date&&(!studentId.HasValue||x.StudentId==studentId.Value)).ToList();
  }
  public static string Csv(IEnumerable<InstallmentReportRow> rows){
   var lines=new List<string>{"InvoiceId,StudentId,Currency,Installment,DueDate,Amount,Paid,Outstanding,Status"};
   foreach(var x in rows)lines.Add(string.Join(",",new[]{x.InvoiceId.ToString(),x.StudentId.ToString(),Safe(x.Currency),x.Sequence.ToString(),x.DueDate.ToString("yyyy-MM-dd"),x.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),x.Paid.ToString(System.Globalization.CultureInfo.InvariantCulture),x.Outstanding.ToString(System.Globalization.CultureInfo.InvariantCulture),Safe(x.Status)}));
   return "\uFEFF"+string.Join("\r\n",lines)+"\r\n";
  }
  private static string Safe(string text){text=text??"";if(text.Length>0&&"=+-@".Contains(text[0]))text="'"+text;return "\""+text.Replace("\"","\"\"")+"\"";}
 }
}
