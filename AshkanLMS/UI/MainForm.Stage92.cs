using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowPaymentReminders(){
   Clear();if(AppSession.Role!="Administrator")return;
   bool fa=AppSession.Persian;
   var card=Card(24,24,1020,660);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.Controls.Add(Header(fa?"مرکز یادآوری سررسید اقساط":"Installment Reminder Center"));
   var daysLabel=new Label{Text=fa?"روزهای آینده":"Days ahead",Location=new Point(24,64),AutoSize=true};card.Controls.Add(daysLabel);
   var days=new NumericUpDown{Location=new Point(135,61),Minimum=0,Maximum=90,Value=7,Width=65};card.Controls.Add(days);
   var currency=new ComboBox{Location=new Point(215,61),Width=85,DropDownStyle=ComboBoxStyle.DropDownList};currency.Items.AddRange(new object[]{"All","IRR","EUR"});currency.SelectedIndex=0;card.Controls.Add(currency);
   var search=new TextBox{Location=new Point(310,61),Width=130};card.Controls.Add(search);
   var draft=Theme.Button(fa?"کپی متن یادآوری":"Copy reminder draft");draft.Location=new Point(455,60);draft.Width=185;card.Controls.Add(draft);
   var mark=Theme.Button(fa?"ثبت بررسی":"Mark reviewed");mark.Location=new Point(650,60);mark.Width=130;card.Controls.Add(mark);
   var email=Theme.Button(fa?"ارسال ایمیل":"Send email");email.Location=new Point(455,95);email.Width=185;card.Controls.Add(email);
   var export=Theme.Button(fa?"خروجی CSV":"Export CSV");export.Location=new Point(790,60);export.Width=130;card.Controls.Add(export);
   var totals=new Label{Location=new Point(24,106),Size=new Size(940,54),Font=Theme.Font(10,FontStyle.Bold)};card.Controls.Add(totals);
   var grid=Grid();grid.Location=new Point(24,170);grid.Size=new Size(940,420);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
   var foot=new Label{Location=new Point(24,602),AutoSize=true,Text=fa?"ثبت بررسی فقط روی همین رایانه ذخیره می‌شود؛ پیامک یا ایمیل ارسال نمی‌شود.":"Reviewed markers are local to this computer; no email or SMS is sent."};card.Controls.Add(foot);
   System.Collections.Generic.List<InstallmentReminder> visible=new System.Collections.Generic.List<InstallmentReminder>();
   Action refresh=()=>{
    try{
     var all=InstallmentReminderService.GetPending(data.TuitionInvoices,data.TuitionInstallments,data.TuitionPayments,DateTime.Today,(int)days.Value);
     int studentId=0;var hasFilter=!string.IsNullOrWhiteSpace(search.Text);
     if(hasFilter&&(!int.TryParse(search.Text,out studentId)||studentId<=0)){totals.Text=fa?"شناسه دانشجو نامعتبر است":"Invalid student ID";grid.DataSource=null;visible.Clear();return;}
     visible=all.Where(x=>(currency.Text=="All"||x.Currency==currency.Text)&&(!hasFilter||x.StudentId==studentId)).ToList();
     var reviewed=ReminderAcknowledgementStore.Read();
     grid.DataSource=visible.Select(x=>new{x.InvoiceId,x.StudentId,x.Sequence,x.DueDate,x.Outstanding,x.Currency,x.Category,Reviewed=reviewed.Contains(x.Key)}).ToList();
     totals.Text=string.Format(fa?"یادآوری‌ها: {0}  |  معوق: {1}  |  امروز: {2}  |  بررسی‌شده: {3}":"Reminders: {0}  |  Overdue: {1}  |  Due today: {2}  |  Reviewed: {3}",visible.Count,visible.Count(x=>x.Category=="Overdue"),visible.Count(x=>x.Category=="Due today"),visible.Count(x=>reviewed.Contains(x.Key)));
     foreach(DataGridViewRow row in grid.Rows){var category=Convert.ToString(row.Cells["Category"].Value);row.DefaultCellStyle.ForeColor=category=="Overdue"?Color.Firebrick:category=="Due today"?Color.DarkOrange:Theme.Navy;}
    }catch(IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}
   };
   Func<InstallmentReminder> selected=()=>grid.CurrentRow==null||grid.CurrentRow.Index<0||grid.CurrentRow.Index>=visible.Count?null:visible[grid.CurrentRow.Index];
   days.ValueChanged+=(s,e)=>refresh();currency.SelectedIndexChanged+=(s,e)=>refresh();search.TextChanged+=(s,e)=>refresh();
   draft.Click+=(s,e)=>{var item=selected();if(item==null)return;try{Clipboard.SetText(InstallmentReminderService.Draft(item,fa));MessageBox.Show(fa?"متن در کلیپ‌بورد کپی شد؛ ارسال نشده است.":"Draft copied to clipboard; nothing was sent.");}catch(System.Runtime.InteropServices.ExternalException ex){MessageBox.Show(ex.Message);}};
   mark.Click+=(s,e)=>{var item=selected();if(item==null)return;try{ReminderAcknowledgementStore.Mark(item.Key);refresh();}catch(IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}};
   export.Click+=(s,e)=>{using(var dlg=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="reminders-"+DateTime.Today.ToString("yyyyMMdd")+".csv"}){if(dlg.ShowDialog(this)==DialogResult.OK){try{File.WriteAllText(dlg.FileName,InstallmentReminderService.Csv(visible),new System.Text.UTF8Encoding(false));}catch(IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}}}};
   email.Click+=(s,e)=>{var item=selected();if(item!=null)ShowReminderEmailDialog(item);};
   refresh();
  }
 }
}
