using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI {
 public partial class MainForm {
  // Dashboard widgets use existing persisted records; no fabricated sample statistics.
  private void AddDashboardPriorityAlerts97(FlowLayoutPanel host,bool fa) {
   var card=AnalyticsCard(fa?"هشدارهای اولویت‌دار":"Priority alerts",485,240);
   var settings=NotificationPreferencesService.Load();
   var message=fa?"اعلان مالی برای این نقش فعال نیست.":"Financial alerts are not available for this role.";
   if(AppSession.Role=="Administrator" && settings.ShowFinancial) {
    var alerts=NotificationCenterService.Financial(data.TuitionInvoices,data.TuitionInstallments,data.TuitionPayments,DateTime.Today,settings.DaysAhead);
    var overdue=alerts.Count(x=>x.Kind=="Overdue");
    var due=alerts.Count(x=>x.Kind=="Due today");
    var upcoming=alerts.Count(x=>x.Kind=="Upcoming");
    message=(fa?"اقساط معوق: ":"Overdue installments: ")+overdue+Environment.NewLine+Environment.NewLine+
      (fa?"سررسید امروز: ":"Due today: ")+due+Environment.NewLine+Environment.NewLine+
      (fa?"سررسیدهای آینده: ":"Upcoming installments: ")+upcoming;
   }
   var label=new Label{Dock=DockStyle.Fill,Padding=new Padding(18),Font=Theme.Font(11),ForeColor=Theme.Navy,Text=message,
    RightToLeft=fa?RightToLeft.Yes:RightToLeft.No,TextAlign=fa?ContentAlignment.TopRight:ContentAlignment.TopLeft};
   card.Controls.Add(label);host.Controls.Add(card);
  }
  private void AddDashboardAcademicPulse97(FlowLayoutPanel host,bool fa) {
   var card=AnalyticsCard(fa?"نمای کلی فعالیت‌های آموزشی":"Academic activity pulse",485,240);
   var today=DateTime.Today;
   var nextWeek=data.Exams.Count(x=>x.Date>=today && x.Date<today.AddDays(7));
   var message=(fa?"آزمون‌های هفت روز آینده: ":"Exams in next 7 days: ")+nextWeek+Environment.NewLine+Environment.NewLine+
     (fa?"تعداد تکالیف: ":"Assignments: ")+data.Assignments.Count+Environment.NewLine+Environment.NewLine+
     (fa?"گواهینامه‌های صادرشده: ":"Certificates issued: ")+data.Certificates.Count;
   card.Controls.Add(new Label{Dock=DockStyle.Fill,Padding=new Padding(18),Font=Theme.Font(11),ForeColor=Theme.Navy,Text=message,
    RightToLeft=fa?RightToLeft.Yes:RightToLeft.No,TextAlign=fa?ContentAlignment.TopRight:ContentAlignment.TopLeft});
   host.Controls.Add(card);
  }
 }
}
