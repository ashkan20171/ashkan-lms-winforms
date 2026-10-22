using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowSecurityCenter() {
   Clear();
   if (AppSession.Role != "Administrator") {
    content.Controls.Add(new Label {Text=AppSession.Persian?"فقط مدیر سیستم مجاز است":"Administrator access required", AutoSize=true, Location=new Point(30,30)});
    return;
   }
   var card=Card(24,24,986,600);
   card.Controls.Add(Header(AppSession.Persian?"مرکز امنیت و تاریخچه رویدادها":"Security Center & Event History"));
   var search=new TextBox {Location=new Point(18,64),Size=new Size(285,30),Font=Theme.Font(10)};
   var searchLabel=new Label {Text=AppSession.Persian?"جست‌وجوی کاربر، رویداد یا موضوع":"Search actor, event or subject",Location=new Point(18,43),AutoSize=true};
   card.Controls.Add(searchLabel);card.Controls.Add(search);
   var from=new DateTimePicker {Location=new Point(320,64),Width=170,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(-30),ShowCheckBox=true,Checked=false};
   var to=new DateTimePicker {Location=new Point(505,64),Width=170,Format=DateTimePickerFormat.Short,Value=DateTime.Today,ShowCheckBox=true,Checked=false};
   card.Controls.Add(from);card.Controls.Add(to);
   var grid=Grid();grid.Location=new Point(18,116);grid.Size=new Size(945,385);card.Controls.Add(grid);
   var status=new Label {Location=new Point(18,517),AutoSize=true,ForeColor=Theme.Muted};card.Controls.Add(status);
   var refresh=Theme.Button(AppSession.Persian?"به‌روزرسانی":"Refresh");refresh.Location=new Point(690,62);refresh.Size=new Size(125,34);card.Controls.Add(refresh);
   var audit=new SecurityAuditService();
   Action load=()=>{
    try {
     var records=audit.Recent(AppSession.Role,1000).AsEnumerable();
     var term=search.Text.Trim();
     if(term.Length>0)records=records.Where(x=>(x.Actor??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||(x.Event??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||(x.Subject??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0);
     if(from.Checked)records=records.Where(x=>x.UtcTime.ToLocalTime().Date>=from.Value.Date);
     if(to.Checked)records=records.Where(x=>x.UtcTime.ToLocalTime().Date<=to.Value.Date);
     var rows=records.Select(x=>new {Time=x.UtcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),Actor=x.Actor,Event=x.Event,Subject=x.Subject}).ToList();
     grid.DataSource=rows;
     status.Text=(AppSession.Persian?"رویدادهای نمایش داده‌شده: ":"Displayed events: ")+rows.Count+" / 1000";
    } catch(Exception ex) {status.Text=AppSession.Persian?"خواندن گزارش ناموفق بود":"Could not read security log";MessageBox.Show(ex.Message,"AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
   };
   refresh.Click+=(s,e)=>load();search.TextChanged+=(s,e)=>load();from.ValueChanged+=(s,e)=>load();to.ValueChanged+=(s,e)=>load();
   load();
  }
 }
}
