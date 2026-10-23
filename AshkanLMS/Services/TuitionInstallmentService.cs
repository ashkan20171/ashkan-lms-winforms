using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class InstallmentBalance { public int Sequence{get;set;} public DateTime DueDate{get;set;} public decimal Amount{get;set;} public decimal Paid{get;set;} public decimal Outstanding{get;set;} public string Status{get;set;} }
 public static class TuitionInstallmentService {
  public static void ValidatePlan(TuitionInvoice invoice,IEnumerable<TuitionInstallment> installments){
   if(invoice==null)throw new ArgumentNullException("invoice");
   var rows=installments.OrderBy(x=>x.Sequence).ToList();
   if(rows.Count<2||rows.Count>24||rows.Any(x=>x.InvoiceId!=invoice.Id||x.Amount<=0m)||rows.Sum(x=>x.Amount)!=invoice.Amount||rows.Select(x=>x.Sequence).Distinct().Count()!=rows.Count)throw new InvalidOperationException("Invalid installment plan");
  }
  // Payments are allocated oldest installment first; the ledger does not imply a payment-to-installment foreign key.
  public static List<InstallmentBalance> Calculate(IEnumerable<TuitionInstallment> installments,IEnumerable<TuitionPayment> payments,DateTime today){
   var rows=installments.OrderBy(x=>x.DueDate).ThenBy(x=>x.Sequence).ToList();
   if(rows.Count==0)return new List<InstallmentBalance>();
   if(rows.Select(x=>x.InvoiceId).Distinct().Count()!=1)throw new ArgumentException("Only one invoice per calculation");
   decimal remaining=payments.Where(x=>x.InvoiceId==rows[0].InvoiceId).Sum(x=>x.Amount);
   var result=new List<InstallmentBalance>();
   foreach(var row in rows){var allocated=Math.Min(row.Amount,Math.Max(0m,remaining));remaining-=allocated;var due=row.Amount-allocated;
    result.Add(new InstallmentBalance{Sequence=row.Sequence,DueDate=row.DueDate,Amount=row.Amount,Paid=allocated,Outstanding=due,Status=due==0m?"Paid":row.DueDate.Date<today.Date?"Overdue":allocated>0m?"Partial":"Upcoming"});
   }
   return result;
  }
 }
}
