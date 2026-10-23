using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI { public partial class MainForm {
 void ShowNotificationPreferences96(){
  Clear();bool fa=AppSession.Persian;var card=Card(24,24,850,490);card.Controls.Add(Header(fa?"تنظیمات اعلان‌ها":"Notification Preferences"));
  var settings=NotificationPreferencesService.Load();
  var daysLabel=new Label{Text=fa?"بازه هشدار سررسید (روز)":"Upcoming reminder window (days)",Location=new Point(24,75),Size=new Size(310,28)};card.Controls.Add(daysLabel);
  var days=new NumericUpDown{Minimum=1,Maximum=90,Value=settings.DaysAhead,Location=new Point(355,75),Width=90};card.Controls.Add(days);
  var finance=new CheckBox{Text=fa?"نمایش هشدارهای مالی":"Show financial alerts",Checked=settings.ShowFinancial,Location=new Point(24,130),Width=330};finance.Enabled=AppSession.Role=="Administrator";card.Controls.Add(finance);
  var activities=new CheckBox{Text=fa?"نمایش فعالیت‌های آموزشی":"Show academic activities",Checked=settings.ShowActivities,Location=new Point(24,175),Width=330};card.Controls.Add(activities);
  var dashboard=new CheckBox{Text=fa?"نمایش خلاصه در داشبورد":"Show summary on dashboard",Checked=settings.ShowDashboardSummary,Location=new Point(24,220),Width=350};card.Controls.Add(dashboard);
  var save=Theme.Button(fa?"ذخیره تنظیمات":"Save preferences");save.Location=new Point(24,275);save.Width=210;card.Controls.Add(save);
  var hint=new Label{Text=fa?"تنظیمات برای کاربر فعلی روی همین رایانه ذخیره می‌شود.":"Preferences are stored for this user on this computer.",Location=new Point(24,340),Size=new Size(770,60)};card.Controls.Add(hint);
  save.Click+=(s,e)=>{try{NotificationPreferencesService.Save(new NotificationPreferences{DaysAhead=(int)days.Value,ShowFinancial=AppSession.Role=="Administrator"&&finance.Checked,ShowActivities=activities.Checked,ShowDashboardSummary=dashboard.Checked});MessageBox.Show(fa?"تنظیمات ذخیره شد.":"Preferences saved.");}catch(System.IO.IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}};
 }
 void AddDashboardNotificationSummary96(){
  var preferences=NotificationPreferencesService.Load();if(!preferences.ShowDashboardSummary)return;
  int overdue=0,upcoming=0;
  if(AppSession.Role=="Administrator"&&preferences.ShowFinancial){var alerts=NotificationCenterService.Financial(data.TuitionInvoices,data.TuitionInstallments,data.TuitionPayments,DateTime.Today,preferences.DaysAhead);overdue=alerts.Count(x=>x.Kind=="Overdue");upcoming=alerts.Count(x=>x.Kind=="Upcoming"||x.Kind=="Due today");}
  var card=Card(24,690,890,95);card.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
  var label=new Label{Location=new Point(18,15),Size=new Size(840,65),Font=Theme.Font(10,FontStyle.Bold),Text=AppSession.Persian?string.Format("اعلان‌ها | اقساط معوق: {0} | سررسید نزدیک: {1}",overdue,upcoming):string.Format("Notifications | Overdue installments: {0} | Upcoming: {1}",overdue,upcoming)};card.Controls.Add(label);
 }
}}
