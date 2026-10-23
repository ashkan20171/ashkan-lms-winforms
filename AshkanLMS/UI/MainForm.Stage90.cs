using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowInstallmentManagement(){
   Clear();
   if(AppSession.Role!="Administrator")return;
   var card=Card(24,24,980,610);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.Controls.Add(Header(AppSession.Persian?"مدیریت اقساط شهریه":"Tuition Installment Plans"));
   var invoiceLabel=new Label{Text=AppSession.Persian?"شناسه صورتحساب":"Invoice ID",Location=new Point(24,74),AutoSize=true};card.Controls.Add(invoiceLabel);
   var invoice=new NumericUpDown{Location=new Point(24,100),Width=125,Minimum=1,Maximum=100000000};card.Controls.Add(invoice);
   var countLabel=new Label{Text=AppSession.Persian?"تعداد اقساط":"Installments",Location=new Point(166,74),AutoSize=true};card.Controls.Add(countLabel);
   var count=new NumericUpDown{Location=new Point(166,100),Width=110,Minimum=2,Maximum=24,Value=3};card.Controls.Add(count);
   var dateLabel=new Label{Text=AppSession.Persian?"سررسید اولین قسط":"First due date",Location=new Point(294,74),AutoSize=true};card.Controls.Add(dateLabel);
   var first=new DateTimePicker{Location=new Point(294,100),Width=160,Format=DateTimePickerFormat.Short};card.Controls.Add(first);
   var create=Theme.Button(AppSession.Persian?"ایجاد برنامه اقساط":"Create installment plan");create.Location=new Point(475,96);create.Width=200;card.Controls.Add(create);
   var summary=new Label{Location=new Point(24,152),Size=new Size(900,45),Font=Theme.Font(10,FontStyle.Bold)};card.Controls.Add(summary);
   var grid=Grid();grid.Location=new Point(24,210);grid.Size=new Size(925,345);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
   Action refresh=()=>{var id=(int)invoice.Value;var rows=TuitionInstallmentService.Calculate(data.TuitionInstallments.Where(x=>x.InvoiceId==id),data.TuitionPayments,DateTime.Today);
    grid.DataSource=rows.Select(x=>new{Installment=x.Sequence,DueDate=x.DueDate,Amount=x.Amount,Paid=x.Paid,Outstanding=x.Outstanding,Status=x.Status}).ToList();
    var bill=data.TuitionInvoices.FirstOrDefault(x=>x.Id==id);
    summary.Text=bill==null?(AppSession.Persian?"صورتحساب یافت نشد":"Invoice not found"):string.Format("{0}: {1}  |  {2}: {3:N2} {4}  |  {5}: {6:N2}",AppSession.Persian?"دانشجو":"Student",bill.StudentId,AppSession.Persian?"مانده اقساط":"Outstanding",rows.Sum(x=>x.Outstanding),bill.Currency,AppSession.Persian?"معوق":"Overdue",rows.Where(x=>x.Status=="Overdue").Sum(x=>x.Outstanding));};
   invoice.ValueChanged+=(s,e)=>refresh();
   create.Click+=(s,e)=>{try{data.CreateInstallmentPlan((int)invoice.Value,(int)count.Value,first.Value.Date);refresh();}catch(Exception ex){MessageBox.Show(ex.Message,"AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
   refresh();
  }
 }
}
