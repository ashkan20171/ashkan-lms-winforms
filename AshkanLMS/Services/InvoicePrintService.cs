using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 /// <summary>Formats an invoice for paginated WinForms printing; does not modify ledger data.</summary>
 public static class InvoicePrintService {
  public static List<string> BuildLines(TuitionInvoice invoice,IEnumerable<TuitionPayment> payments,bool persian){
   if(invoice==null)throw new ArgumentNullException("invoice");
   if(payments==null)throw new ArgumentNullException("payments");
   var paid=payments.Where(p=>p.InvoiceId==invoice.Id).OrderBy(p=>p.PaidAt).ThenBy(p=>p.Id).ToList();
   Func<string,string,string> label=(fa,en)=>persian?fa:en;
   var result=new List<string>{
    label("شماره صورتحساب: ","Invoice number: ")+invoice.Id,
    label("شناسه دانشجو: ","Student ID: ")+invoice.StudentId,
    label("شناسه دوره: ","Course ID: ")+invoice.CourseId,
    label("سررسید: ","Due date: ")+invoice.DueDate.ToString("yyyy-MM-dd"),
    label("واحد پول: ","Currency: ")+invoice.Currency,
    label("مبلغ صورتحساب: ","Invoice total: ")+invoice.Amount.ToString("N2"),
    label("مجموع پرداخت‌ها: ","Paid total: ")+paid.Sum(p=>p.Amount).ToString("N2"),
    label("مانده: ","Outstanding: ")+(invoice.Amount-paid.Sum(p=>p.Amount)).ToString("N2"),
    label("جزئیات پرداخت‌ها:","Payment details:")
   };
   if(paid.Count==0)result.Add(label("پرداختی ثبت نشده است.","No payments recorded."));
   foreach(var payment in paid){
    var reference=(payment.Reference??string.Empty).Replace("\r"," ").Replace("\n"," ");
    result.Add(payment.PaidAt.ToString("yyyy-MM-dd")+" | "+payment.Amount.ToString("N2")+" "+invoice.Currency+" | "+reference);
   }
   return result;
  }
  public static int PageNumber(int totalLines,int printedLines,int linesPerPage){
   if(totalLines<0||printedLines<0||linesPerPage<=0)throw new ArgumentOutOfRangeException("pagination");
   return Math.Max(1,(Math.Max(1,printedLines)-1)/linesPerPage+1);
  }
 }
}
