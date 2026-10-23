using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI { public partial class MainForm {
 void ShowNotificationCenter95(){
  Clear();bool fa=AppSession.Persian;var card=Card(24,24,1020,650);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
  card.Controls.Add(Header(fa?"مرکز اعلان‌ها":"Notification Center"));
  var summary=new Label{Location=new Point(24,62),Size=new Size(820,35),Font=Theme.Font(11,FontStyle.Bold)};card.Controls.Add(summary);
  var kind=new ComboBox{Location=new Point(24,103),Width=190,DropDownStyle=ComboBoxStyle.DropDownList};kind.Items.AddRange(new object[]{fa?"همه":"All",fa?"معوق":"Overdue",fa?"امروز":"Due today",fa?"آینده":"Upcoming",fa?"فعالیت‌ها":"Activities"});kind.SelectedIndex=0;card.Controls.Add(kind);
  var unreadOnly=new CheckBox{Text=fa?"فقط خوانده‌نشده":"Unread only",Location=new Point(230,107),Width=180};card.Controls.Add(unreadOnly);
  var mark=Theme.Button(fa?"علامت‌گذاری خوانده‌شده":"Mark selected read");mark.Location=new Point(430,102);mark.Width=205;card.Controls.Add(mark);
  var allRead=Theme.Button(fa?"خواندن همه موارد":"Mark all read");allRead.Location=new Point(650,102);allRead.Width=180;card.Controls.Add(allRead);
  var grid=Grid();grid.Location=new Point(24,150);grid.Size=new Size(940,430);grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(grid);
  var note=new Label{Location=new Point(24,594),Size=new Size(930,40),Text=fa?"اعلان‌های مالی از اقساط ثبت‌شده محاسبه می‌شوند. وضعیت خوانده‌شده فقط روی این رایانه ذخیره می‌شود.":"Financial alerts use saved installment data. Read state is stored on this computer only."};card.Controls.Add(note);
  System.Collections.Generic.List<CenterNotification> all=new System.Collections.Generic.List<CenterNotification>();System.Collections.Generic.List<CenterNotification> visible=new System.Collections.Generic.List<CenterNotification>();
  Action refresh=()=>{try{
   var preferences=NotificationPreferencesService.Load();
   all.Clear();if(AppSession.Role=="Administrator"&&preferences.ShowFinancial)all.AddRange(NotificationCenterService.Financial(data.TuitionInvoices,data.TuitionInstallments,data.TuitionPayments,DateTime.Today,preferences.DaysAhead));
   foreach(var activity in data.Activities){if(!preferences.ShowActivities)break;all.Add(new CenterNotification{Key="activity:"+activity.At.Ticks+":"+activity.TypeKey+":"+activity.TextKey,Kind="Activities",Title=Localization.T(activity.TypeKey),Detail=Localization.T(activity.TextKey),At=activity.At});}
   var read=NotificationReadStore.Load();visible=all.Where(x=>(kind.SelectedIndex==0||x.Kind==new[]{"All","Overdue","Due today","Upcoming","Activities"}[kind.SelectedIndex])&&(!unreadOnly.Checked||!read.Contains(x.Key))).OrderBy(x=>x.Kind=="Overdue"?0:x.Kind=="Due today"?1:2).ThenByDescending(x=>x.At).ToList();
   grid.DataSource=visible.Select(x=>new{Status=read.Contains(x.Key)?(fa?"خوانده‌شده":"Read"):(fa?"جدید":"New"),Category=x.Kind,Subject=x.Title,Details=x.Detail,Date=x.At.ToString("yyyy-MM-dd")}).ToList();
   summary.Text=string.Format(fa?"اعلان‌ها: {0}  |  خوانده‌نشده: {1}  |  اقساط معوق: {2}":"Notifications: {0}  |  Unread: {1}  |  Overdue: {2}",all.Count,NotificationCenterService.UnreadCount(all,read),all.Count(x=>x.Kind=="Overdue"));
   foreach(DataGridViewRow r in grid.Rows){if(r.Index<visible.Count)r.DefaultCellStyle.ForeColor=visible[r.Index].Kind=="Overdue"?Color.Firebrick:visible[r.Index].Kind=="Due today"?Color.DarkOrange:Theme.Navy;}
  }catch(System.IO.IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}};
  kind.SelectedIndexChanged+=(s,e)=>refresh();unreadOnly.CheckedChanged+=(s,e)=>refresh();
  mark.Click+=(s,e)=>{if(grid.CurrentRow==null||grid.CurrentRow.Index>=visible.Count)return;try{NotificationReadStore.Mark(new[]{visible[grid.CurrentRow.Index].Key});refresh();}catch(System.IO.IOException ex){MessageBox.Show(ex.Message);}};
  allRead.Click+=(s,e)=>{try{NotificationReadStore.Mark(visible.Select(x=>x.Key));refresh();}catch(System.IO.IOException ex){MessageBox.Show(ex.Message);}};
  refresh();
 }
}}
