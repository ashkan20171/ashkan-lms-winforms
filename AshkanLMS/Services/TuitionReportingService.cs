using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class MonthlyRevenue { public int Year {get;set;} public int Month {get;set;} public decimal Amount {get;set;} }
 public sealed class DebtorRecord { public int InvoiceId {get;set;} public int StudentId {get;set;} public decimal Outstanding {get;set;} public DateTime DueDate {get;set;} }
 public static class TuitionReportingService {
  public static List<MonthlyRevenue> MonthlyPayments(IEnumerable<TuitionInvoice> invoices,IEnumerable<TuitionPayment> payments,string currency,DateTime through) {
   if(invoices==null||payments==null)throw new ArgumentNullException("ledger");
   var ids=new HashSet<int>(invoices.Where(i=>i.Currency==currency).Select(i=>i.Id));
   var sums=payments.Where(p=>ids.Contains(p.InvoiceId)&&p.PaidAt<=through).GroupBy(p=>new {p.PaidAt.Year,p.PaidAt.Month}).ToDictionary(g=>g.Key.Year*100+g.Key.Month,g=>g.Sum(p=>p.Amount));
   var first=new DateTime(through.Year,through.Month,1).AddMonths(-5);
   return Enumerable.Range(0,6).Select(n=>first.AddMonths(n)).Select(d=>new MonthlyRevenue {Year=d.Year,Month=d.Month,Amount=sums.ContainsKey(d.Year*100+d.Month)?sums[d.Year*100+d.Month]:0m}).ToList();
  }
  public static List<DebtorRecord> Debtors(IEnumerable<TuitionInvoice> invoices,IEnumerable<TuitionPayment> payments,string currency,DateTime asOf) {
   if(invoices==null||payments==null)throw new ArgumentNullException("ledger");
   var totals=payments.Where(p=>p.PaidAt<=asOf).GroupBy(p=>p.InvoiceId).ToDictionary(g=>g.Key,g=>g.Sum(p=>p.Amount));
   return invoices.Where(i=>i.Currency==currency).Select(i=>new DebtorRecord {InvoiceId=i.Id,StudentId=i.StudentId,DueDate=i.DueDate,Outstanding=i.Amount-(totals.ContainsKey(i.Id)?totals[i.Id]:0m)}).Where(x=>x.Outstanding>0m).OrderBy(x=>x.DueDate).ThenBy(x=>x.InvoiceId).ToList();
  }
 }
}
