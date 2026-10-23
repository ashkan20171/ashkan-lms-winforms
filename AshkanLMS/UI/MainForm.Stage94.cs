using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowReminderEmailHistory(){
   Clear();if(AppSession.Role!="Administrator")return;
   bool fa=AppSession.Persian;
   var card=Card(24,24,1020,660);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.Controls.Add(Header(fa?"تاریخچه ارسال ایمیل":"Email reminder history"));
   var search=new TextBox{Location=new Point(24,70),Width=260};card.Controls.Add(search);
   var status=new ComboBox{Location=new Point(300,70),Width=180,DropDownStyle=ComboBoxStyle.DropDownList};status.Items.AddRange(new object[]{"All","Accepted by SMTP","Failed"});status.SelectedIndex=0;card.Controls.Add(status);
   var count=new Label{Location=new Point(500,73),AutoSize=true};card.Controls.Add(count);
   var grid=Grid();grid.Location=new Point(24,120);grid.Size=new Size(940,470);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
   Action refresh=()=>{
    try{
     var accepted=ReminderEmailService.ReadHistory().Select(line=>line.Split('\t')).Where(p=>p.Length>=3).Select(p=>new{Date=p[0],Installment=p[1],Recipient=p[2],Status="Accepted by SMTP"});
     var failed=ReminderEmailService.ReadFailures().Select(line=>line.Split('\t')).Where(p=>p.Length>=4).Select(p=>new{Date=p[0],Installment=p[1],Recipient=p[2],Status="Failed"});
     var rows=accepted.Concat(failed).Where(p=>(status.Text=="All"||p.Status==status.Text)&&(string.IsNullOrWhiteSpace(search.Text)||p.Installment.IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0||p.Recipient.IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0)).OrderByDescending(p=>p.Date).ToList();
     grid.DataSource=rows;count.Text=(fa?"تعداد سوابق: ":"Records: ")+rows.Count;
     foreach(DataGridViewRow row in grid.Rows)if(Convert.ToString(row.Cells["Status"].Value)=="Failed")row.DefaultCellStyle.ForeColor=Color.Firebrick;
    }catch(System.IO.IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}
   };
   search.TextChanged+=(s,e)=>refresh();status.SelectedIndexChanged+=(s,e)=>refresh();refresh();
  }
 }
}
