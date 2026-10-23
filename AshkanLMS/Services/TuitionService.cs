using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class TuitionSummary { public decimal Invoiced{get;set;} public decimal Paid{get;set;} public decimal Outstanding{get;set;} public int OverdueCount{get;set;} }
 public static class TuitionService {
  public static void ValidateInvoice(TuitionInvoice invoice, IEnumerable<Student> students, IEnumerable<Course> courses) {
   if(invoice==null)throw new ArgumentNullException("invoice");
   if(invoice.Amount<=0m || invoice.Amount>100000000000m)throw new ArgumentOutOfRangeException("Amount");
   if(!string.Equals(invoice.Currency,"IRR",StringComparison.Ordinal) && !string.Equals(invoice.Currency,"EUR",StringComparison.Ordinal))throw new ArgumentException("Unsupported currency");
   if(!students.Any(x=>x.Id==invoice.StudentId)||!courses.Any(x=>x.Id==invoice.CourseId))throw new InvalidOperationException("Student or course not found");
  }
  public static void ValidatePayment(TuitionPayment payment, IEnumerable<TuitionInvoice> invoices, IEnumerable<TuitionPayment> payments) {
   if(payment==null)throw new ArgumentNullException("payment");
   var invoice=invoices.SingleOrDefault(x=>x.Id==payment.InvoiceId);
   if(invoice==null)throw new InvalidOperationException("Invoice not found");
   if(payment.Amount<=0m)throw new ArgumentOutOfRangeException("Amount");
   if(string.IsNullOrWhiteSpace(payment.Reference)||payment.Reference.Trim().Length>100)throw new ArgumentException("Payment reference required");
   if(payments.Any(x=>string.Equals(x.Reference,payment.Reference,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Duplicate payment reference");
   if(payments.Where(x=>x.InvoiceId==invoice.Id).Sum(x=>x.Amount)+payment.Amount>invoice.Amount)throw new InvalidOperationException("Payment exceeds invoice balance");
  }
  public static TuitionSummary Summarize(IEnumerable<TuitionInvoice> invoices, IEnumerable<TuitionPayment> payments, string currency, DateTime today) {
   var items=invoices.Where(x=>x.Currency==currency).ToList();var ids=new HashSet<int>(items.Select(x=>x.Id));
   var paid=payments.Where(x=>ids.Contains(x.InvoiceId)).GroupBy(x=>x.InvoiceId).ToDictionary(x=>x.Key,x=>x.Sum(p=>p.Amount));
   return new TuitionSummary { Invoiced=items.Sum(x=>x.Amount),Paid=paid.Values.Sum(),Outstanding=items.Sum(x=>x.Amount)-paid.Values.Sum(),OverdueCount=items.Count(x=>x.DueDate.Date<today.Date && x.Amount-(paid.ContainsKey(x.Id)?paid[x.Id]:0m)>0m) };
  }
 }
}
