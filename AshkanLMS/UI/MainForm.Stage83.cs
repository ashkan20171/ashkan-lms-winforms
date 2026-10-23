using System;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private static string CsvSafe(string value){
   value=value??"";if(value.Length>0 && "=+-@".IndexOf(value[0])>=0)value="'"+value;
   return "\""+value.Replace("\"","\"\"")+"\"";
  }
  private void ShowTuitionManagement(){
   Clear();
   if(AppSession.Role!="Administrator"){content.Controls.Add(new Label{Text=AppSession.Persian?"دسترسی مالی فقط برای مدیر مجاز است":"Finance is restricted to administrators",AutoSize=true,Location=new Point(24,24)});return;}
   var card=Card(24,24,980,630);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.Controls.Add(Header(AppSession.Persian?"مدیریت شهریه و پرداخت‌ها":"Tuition & Payments"));
   var currency=new ComboBox{Location=new Point(22,58),Width=120,DropDownStyle=ComboBoxStyle.DropDownList};currency.Items.AddRange(new object[]{"IRR","EUR"});currency.SelectedIndex=0;card.Controls.Add(currency);
   var summary=new Label{Location=new Point(160,60),Size=new Size(760,48),Font=Theme.Font(10,FontStyle.Bold)};card.Controls.Add(summary);
   var search=new TextBox{Location=new Point(22,90),Width=225};card.Controls.Add(search);
   var overdueOnly=new CheckBox{Location=new Point(22,112),Width=260,Height=20,Text=AppSession.Persian?"فقط صورتحساب‌های معوق":"Overdue invoices only"};card.Controls.Add(overdueOnly);
   var searchLabel=new Label{Text=AppSession.Persian?"جست‌وجو با شناسه دانشجو یا صورتحساب":"Search student or invoice ID",Location=new Point(260,92),AutoSize=true};card.Controls.Add(searchLabel);
   var from=new DateTimePicker{Location=new Point(505,88),Width=145,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddYears(-1)};card.Controls.Add(from);
   var until=new DateTimePicker{Location=new Point(660,88),Width=145,Format=DateTimePickerFormat.Short,Value=DateTime.Today};card.Controls.Add(until);
   var export=Theme.Button("Export CSV");export.Location=new Point(820,86);export.Width=130;card.Controls.Add(export);
   var grid=Grid();grid.Location=new Point(22,142);grid.Size=new Size(930,323);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
   var addInvoice=Theme.Button(AppSession.Persian?"صدور صورتحساب":"Create invoice");addInvoice.Location=new Point(22,500);addInvoice.Width=170;card.Controls.Add(addInvoice);
   var recordPayment=Theme.Button(AppSession.Persian?"ثبت پرداخت":"Record payment");recordPayment.Location=new Point(208,500);recordPayment.Width=170;card.Controls.Add(recordPayment);
   var student=new NumericUpDown{Location=new Point(22,555),Minimum=1,Maximum=100000000,Width=110};card.Controls.Add(student);
   var course=new NumericUpDown{Location=new Point(145,555),Minimum=1,Maximum=100000000,Width=110};card.Controls.Add(course);
   var invoiceId=new NumericUpDown{Location=new Point(270,555),Minimum=1,Maximum=100000000,Width=110};card.Controls.Add(invoiceId);
   var amount=new NumericUpDown{Location=new Point(395,555),Minimum=0,Maximum=1000000000,DecimalPlaces=2,Width=145};card.Controls.Add(amount);
   var reference=new TextBox{Location=new Point(555,555),Width=210};card.Controls.Add(reference);
   var due=new DateTimePicker{Location=new Point(780,555),Width=170,Format=DateTimePickerFormat.Short};card.Controls.Add(due);
   var help=new Label{Location=new Point(22,480),AutoSize=true,Text=AppSession.Persian?"شناسه دانشجو | شناسه دوره | شناسه صورتحساب | مبلغ | کد پیگیری | سررسید":"Student ID | Course ID | Invoice ID | Amount | Reference | Due date"};card.Controls.Add(help);
   Action refresh=()=>{var c=currency.Text;var result=TuitionService.Summarize(data.TuitionInvoices,data.TuitionPayments,c,DateTime.Today);summary.Text=string.Format("{0}: {1:N2}  |  {2}: {3:N2}  |  {4}: {5:N2}  |  {6}: {7}",AppSession.Persian?"صورتحساب":"Invoiced",result.Invoiced,AppSession.Persian?"پرداخت":"Paid",result.Paid,AppSession.Persian?"مانده":"Balance",result.Outstanding,AppSession.Persian?"معوق":"Overdue",result.OverdueCount);var q=(search.Text??"").Trim();grid.DataSource=data.TuitionInvoices.Where(x=>x.Currency==c && x.DueDate.Date>=from.Value.Date && x.DueDate.Date<=until.Value.Date && (!overdueOnly.Checked || (x.DueDate.Date<DateTime.Today && x.Amount>data.TuitionPayments.Where(p=>p.InvoiceId==x.Id).Sum(p=>p.Amount))) && (q.Length==0 || x.StudentId.ToString().Contains(q) || x.Id.ToString().Contains(q)))
    .Select(x=>new {x.Id,x.StudentId,x.CourseId,x.Amount,x.DueDate,x.Currency,Paid=data.TuitionPayments.Where(p=>p.InvoiceId==x.Id).Sum(p=>p.Amount),Balance=x.Amount-data.TuitionPayments.Where(p=>p.InvoiceId==x.Id).Sum(p=>p.Amount),Status=TuitionVisualStatus.GetStatus(x.Amount,data.TuitionPayments.Where(p=>p.InvoiceId==x.Id).Sum(p=>p.Amount),x.DueDate,DateTime.Today)}).ToList();TuitionVisualStatus.StyleGrid(grid,AppSession.Persian);};
   overdueOnly.CheckedChanged+=(s,e)=>refresh();currency.SelectedIndexChanged+=(s,e)=>refresh();search.TextChanged+=(s,e)=>refresh();from.ValueChanged+=(s,e)=>refresh();until.ValueChanged+=(s,e)=>refresh();
   export.Click+=(s,e)=>{try{using(var dialog=new SaveFileDialog{Filter="CSV (*.csv)|*.csv",FileName="AshkanLMS-tuition.csv",OverwritePrompt=true}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;
     var lines=new System.Collections.Generic.List<string>{"InvoiceId,StudentId,CourseId,Amount,DueDate,Currency,Paid,Balance,Status"};
     foreach(DataGridViewRow r in grid.Rows){if(r.IsNewRow)continue;lines.Add(string.Join(",",Enumerable.Range(0,9).Select(i=>CsvSafe(Convert.ToString(r.Cells[i].Value)))));}
     File.WriteAllLines(dialog.FileName,lines,new UTF8Encoding(true));}}catch(IOException){MessageBox.Show(AppSession.Persian?"خطا در ذخیره فایل":"Could not save CSV");}catch(UnauthorizedAccessException){MessageBox.Show(AppSession.Persian?"دسترسی به فایل مجاز نیست":"File access denied");}};
   addInvoice.Click+=(s,e)=>{try{data.AddTuitionInvoice((int)student.Value,(int)course.Value,amount.Value,due.Value.Date,currency.Text);refresh();}catch(Exception ex){MessageBox.Show(ex.Message,"AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
   recordPayment.Click+=(s,e)=>{try{data.AddTuitionPayment((int)invoiceId.Value,amount.Value,reference.Text);refresh();}catch(Exception ex){MessageBox.Show(ex.Message,"AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
   refresh();
  }
 }
}
