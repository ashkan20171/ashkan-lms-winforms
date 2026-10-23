using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowInstallmentReports(){
   Clear();if(AppSession.Role!="Administrator")return;
   bool fa=AppSession.Persian;
   var card=Card(24,24,1020,660);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.Controls.Add(Header(fa?"داشبورد و گزارش اقساط":"Installment Analytics & Reports"));
   var currency=new ComboBox{Location=new Point(24,65),Width=100,DropDownStyle=ComboBoxStyle.DropDownList};currency.Items.AddRange(new object[]{"IRR","EUR"});currency.SelectedIndex=0;card.Controls.Add(currency);
   var status=new ComboBox{Location=new Point(138,65),Width=125,DropDownStyle=ComboBoxStyle.DropDownList};status.Items.AddRange(new object[]{"All","Paid","Partial","Upcoming","Overdue"});status.SelectedIndex=0;card.Controls.Add(status);
   var from=new DateTimePicker{Location=new Point(275,65),Width=140,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddMonths(-3)};card.Controls.Add(from);
   var until=new DateTimePicker{Location=new Point(428,65),Width=140,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddMonths(6)};card.Controls.Add(until);
   var student=new TextBox{Location=new Point(582,65),Width=120};student.Text="";card.Controls.Add(student);
   var hint=new Label{Location=new Point(582,43),AutoSize=true,Text=fa?"شناسه دانشجو":"Student ID"};card.Controls.Add(hint);
   var export=Theme.Button(fa?"خروجی CSV":"Export CSV");export.Location=new Point(716,63);export.Width=130;card.Controls.Add(export);
   var totals=new Label{Location=new Point(24,112),Size=new Size(930,60),Font=Theme.Font(11,FontStyle.Bold)};card.Controls.Add(totals);
   var grid=Grid();grid.Location=new Point(24,184);grid.Size=new Size(940,415);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
   System.Collections.Generic.List<InstallmentReportRow> visible=null;
   Action refresh=()=>{
    int id;int? filterId=null;if(!string.IsNullOrWhiteSpace(student.Text)){if(!int.TryParse(student.Text,out id)||id<=0){totals.Text=fa?"شناسه دانشجو باید عدد مثبت باشد":"Student ID must be a positive number";grid.DataSource=null;visible=null;return;}filterId=id;}
    if(from.Value.Date>until.Value.Date){totals.Text=fa?"بازه تاریخ نامعتبر است":"Invalid date range";grid.DataSource=null;visible=null;return;}
    var all=InstallmentReportingService.Build(data.TuitionInvoices,data.TuitionInstallments,data.TuitionPayments,DateTime.Today);
    visible=InstallmentReportingService.Filter(all,currency.Text,status.Text,from.Value,until.Value,filterId);
    grid.DataSource=visible.Select(x=>new{x.InvoiceId,x.StudentId,x.Sequence,x.DueDate,x.Amount,x.Paid,x.Outstanding,x.Status}).ToList();
    totals.Text=string.Format(fa?"اقساط: {0}    |    پرداخت‌شده: {1:N2}    |    مانده: {2:N2}    |    معوق: {3}    |    سررسید ۷ روز آینده: {4}":"Installments: {0}    |    Paid: {1:N2}    |    Due: {2:N2}    |    Overdue: {3}    |    Due next 7 days: {4}",visible.Count,visible.Sum(x=>x.Paid),visible.Sum(x=>x.Outstanding),visible.Count(x=>x.Status=="Overdue"),visible.Count(x=>x.Outstanding>0&&x.DueDate.Date>=DateTime.Today&&x.DueDate.Date<=DateTime.Today.AddDays(7)));
    foreach(DataGridViewRow r in grid.Rows){var value=Convert.ToString(r.Cells["Status"].Value);r.DefaultCellStyle.ForeColor=value=="Overdue"?Color.Firebrick:value=="Paid"?Color.ForestGreen:value=="Partial"?Color.DarkOrange:Theme.Navy;}
   };
   currency.SelectedIndexChanged+=(s,e)=>refresh();status.SelectedIndexChanged+=(s,e)=>refresh();from.ValueChanged+=(s,e)=>refresh();until.ValueChanged+=(s,e)=>refresh();student.TextChanged+=(s,e)=>refresh();
   export.Click+=(s,e)=>{if(visible==null)return;using(var dlg=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="installments-"+DateTime.Today.ToString("yyyyMMdd")+".csv"}){if(dlg.ShowDialog(this)==DialogResult.OK){try{File.WriteAllText(dlg.FileName,InstallmentReportingService.Csv(visible),new System.Text.UTF8Encoding(true));MessageBox.Show(fa?"گزارش ذخیره شد":"Report saved");}catch(IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}}}};
   refresh();
  }
 }
}
