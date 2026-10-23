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
  private void ShowSecurityCenter() {
   Clear();
   if (AppSession.Role != "Administrator") {
    content.Controls.Add(new Label {Text=AppSession.Persian?"فقط مدیر سیستم مجاز است":"Administrator access required", AutoSize=true, Location=new Point(30,30)});
    return;
   }
   var card=Card(24,24,986,650);
   card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.MinimumSize=new Size(740,500);
   content.Resize+=(sender,args)=>{card.Width=Math.Max(740,content.ClientSize.Width-48);card.Height=Math.Max(500,content.ClientSize.Height-48);};
   card.Controls.Add(Header(AppSession.Persian?"مرکز امنیت و تاریخچه رویدادها":"Security Center & Event History"));
   var search=new TextBox {Location=new Point(18,64),Size=new Size(285,30),Font=Theme.Font(10)};
   var searchLabel=new Label {Text=AppSession.Persian?"جست‌وجوی کاربر، رویداد یا موضوع":"Search actor, event or subject",Location=new Point(18,43),AutoSize=true};
   card.Controls.Add(searchLabel);card.Controls.Add(search);
   var from=new DateTimePicker {Location=new Point(320,64),Width=170,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(-30),ShowCheckBox=true,Checked=false};
   var to=new DateTimePicker {Location=new Point(505,64),Width=170,Format=DateTimePickerFormat.Short,Value=DateTime.Today,ShowCheckBox=true,Checked=false};
   card.Controls.Add(from);card.Controls.Add(to);
   var metricPanel=new TableLayoutPanel {Location=new Point(18,110),Size=new Size(945,80),ColumnCount=5,RowCount=1,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Theme.Card};
   for(int i=0;i<5;i++)metricPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20f));
   card.Controls.Add(metricPanel);
   var metricLabels=new Label[5];
   for(int i=0;i<5;i++){
    var tile=new Panel {Dock=DockStyle.Fill,Margin=new Padding(4),BackColor=i==1?Color.FromArgb(255,242,237):i==2?Color.FromArgb(255,246,226):Color.FromArgb(237,244,253)};
    var value=new Label {Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=Theme.Font(10,FontStyle.Bold),ForeColor=i==1?Color.FromArgb(159,56,34):Theme.Text,AutoEllipsis=true};
    tile.Controls.Add(value);metricPanel.Controls.Add(tile,i,0);metricLabels[i]=value;
   }
   var trend=new SecurityTrendChart {Location=new Point(18,200),Size=new Size(945,125),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};card.Controls.Add(trend);
   var grid=Grid();grid.Location=new Point(18,340);grid.Size=new Size(945,240);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
   var export=Theme.Button(AppSession.Persian?"خروجی CSV":"Export CSV");export.Location=new Point(825,62);export.Size=new Size(138,34);card.Controls.Add(export);
   var snapshot=new System.Collections.Generic.List<SecurityAuditService.Entry>();
   var status=new Label {Location=new Point(18,595),AutoSize=true,ForeColor=Theme.Muted};card.Controls.Add(status);
   var refresh=Theme.Button(AppSession.Persian?"به‌روزرسانی":"Refresh");refresh.Location=new Point(690,62);refresh.Size=new Size(125,34);card.Controls.Add(refresh);
   var audit=new SecurityAuditService();
   Action load=()=>{
    try {
     var records=audit.Recent(AppSession.Role,1000).AsEnumerable();
     var term=search.Text.Trim();
     if(term.Length>0)records=records.Where(x=>(x.Actor??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||(x.Event??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||(x.Subject??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0);
     if(from.Checked)records=records.Where(x=>x.UtcTime.ToLocalTime().Date>=from.Value.Date);
     if(to.Checked)records=records.Where(x=>x.UtcTime.ToLocalTime().Date<=to.Value.Date);
     snapshot=records.ToList();
     var m=SecurityDashboardService.Aggregate(snapshot);
     var titles=AppSession.Persian?new[]{"کل رویدادها","ورود ناموفق","قفل حساب","انقضای نشست","لغو نشست"}:new[]{"All events","Failed logins","Locked accounts","Idle timeouts","Revoked sessions"};
     var values=new[]{m.Total,m.FailedLogins,m.LockedAccounts,m.ExpiredSessions,m.RevokedSessions};
     for(int i=0;i<5;i++)metricLabels[i].Text=titles[i]+Environment.NewLine+values[i].ToString();
     trend.SetEvents(snapshot);
     var rows=records.Select(x=>new {Time=x.UtcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),Actor=x.Actor,Event=x.Event,Subject=x.Subject}).ToList();
     grid.DataSource=rows;
     status.Text=(AppSession.Persian?"رویدادهای نمایش داده‌شده: ":"Displayed events: ")+rows.Count+" / 1000";
    } catch(Exception) {status.Text=AppSession.Persian?"خواندن گزارش ناموفق بود":"Could not read security log";MessageBox.Show(AppSession.Persian?"خواندن رویدادهای امنیتی ممکن نشد.":"Unable to read security events.","AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
   };
   export.Click+=(sender,args)=>{try {AuthorizationService.RequireAccountAdmin();using(var save=new SaveFileDialog {Filter="CSV (*.csv)|*.csv",FileName="AshkanLMS-security-events.csv",OverwritePrompt=true}) {if(save.ShowDialog(this)==DialogResult.OK)File.WriteAllText(save.FileName,SecurityDashboardService.ExportCsv(snapshot),new UTF8Encoding(true));}}catch(IOException){MessageBox.Show(AppSession.Persian?"ذخیره گزارش ناموفق بود":"Unable to save report");}catch(UnauthorizedAccessException){MessageBox.Show(AppSession.Persian?"دسترسی مجاز نیست":"Access denied");}};
   refresh.Click+=(s,e)=>load();search.TextChanged+=(s,e)=>load();from.ValueChanged+=(s,e)=>load();to.ValueChanged+=(s,e)=>load();
   load();
  }
 }
}
