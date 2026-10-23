using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI {
 public partial class MainForm {
  FlowLayoutPanel navigationList98;
  TextBox navigationSearch98;
  readonly Dictionary<string,string> navigationGroups98=new Dictionary<string,string> {
   {"Dashboard","Home"},{"GlobalSearch","Home"},{"Notifications","Home"},{"NotificationPreferences","Home"},
   {"Students","Academic"},{"Student360","Academic"},{"RiskCenter","Academic"},{"Courses","Academic"},{"Course360","Academic"},
   {"Instructors","Academic"},{"InstructorWorkspace","Academic"},{"Enrollments","Academic"},{"Attendance","Academic"},
   {"Assignments","Academic"},{"Exams","Academic"},{"Grades","Academic"},{"Certificates","Academic"},{"Calendar","Academic"},
   {"Tuition","Finance"},{"Installments","Finance"},{"InstallmentReports","Finance"},{"PaymentReminders","Finance"},
   {"EmailHistory","Finance"},{"FinanceReports","Finance"},{"Reports","Finance"},
   {"Appearance","Administration"},{"SecurityCenter","Administration"},{"Users","Administration"},{"Audit","Administration"},
   {"RelationalWorkspace","Administration"},{"SupportDiagnostics","Administration"},{"Settings","Administration"}
  };
  string NavigationGroupTitle98(string group) {
   if(!AppSession.Persian)return group;
   switch(group){case "Home":return "خانه و اعلان‌ها";case "Academic":return "آموزش";case "Finance":return "امور مالی و گزارش‌ها";default:return "مدیریت سیستم";}
  }
  void BuildNavigation98(){
   nav.Controls.Clear();
   var brand=new Label{Text="A  |  AshkanLMS",Dock=DockStyle.Top,Height=66,ForeColor=Color.White,Font=Theme.Font(16,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter};
   nav.Controls.Add(brand);
   var searchHolder=new Panel{Dock=DockStyle.Top,Height=54,Padding=new Padding(12,7,12,9),BackColor=Theme.Navy};
   navigationSearch98=new TextBox{Dock=DockStyle.Fill,Font=Theme.Font(10),BorderStyle=BorderStyle.FixedSingle,RightToLeft=AppSession.Persian?RightToLeft.Yes:RightToLeft.No};
   navigationSearch98.AccessibleName=AppSession.Persian?"جست‌وجوی منو":"Search navigation";
   navigationSearch98.TextChanged+=(sender,args)=>RenderNavigation98();
   searchHolder.Controls.Add(navigationSearch98);nav.Controls.Add(searchHolder);
   navigationList98=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(8,8,8,12),BackColor=Theme.Navy};
   navigationList98.SizeChanged+=(sender,args)=>ResizeNavigationItems98();
   nav.Controls.Add(navigationList98);navigationList98.BringToFront();
   // Docking order: list fills space beneath brand and search.
   nav.Controls.SetChildIndex(navigationList98,0);
   nav.Controls.SetChildIndex(searchHolder,1);
   nav.Controls.SetChildIndex(brand,2);
   RenderNavigation98();
  }
  void RenderNavigation98(){
   if(navigationList98==null||navigationList98.IsDisposed)return;
   string filter=(navigationSearch98==null?"":navigationSearch98.Text).Trim();
   navigationList98.SuspendLayout();navigationList98.Controls.Clear();
   foreach(string group in new[]{"Home","Academic","Finance","Administration"}){
    var keys=menuKeys.Where(k=>IsPageAuthorized(k)&&navigationGroups98.ContainsKey(k)&&navigationGroups98[k]==group)
      .Where(k=>filter.Length==0||(k=="Appearance"?(AppSession.Persian?"ظاهر و تم":"Appearance & theme"):Localization.T(k)).IndexOf(filter,StringComparison.CurrentCultureIgnoreCase)>=0||k.IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
    if(keys.Length==0)continue;
    var heading=new Label{Text=NavigationGroupTitle98(group),Height=30,ForeColor=Color.FromArgb(153,177,208),Font=Theme.Font(9,FontStyle.Bold),TextAlign=AppSession.Persian?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft,Margin=new Padding(7,12,7,3)};
    navigationList98.Controls.Add(heading);
    foreach(string key in keys){
     var button=new Button{Text=key=="Appearance"?(AppSession.Persian?"ظاهر و تم":"Appearance & theme"):Localization.T(key),Tag=key,Height=38,FlatStyle=FlatStyle.Flat,ForeColor=Color.FromArgb(230,237,248),BackColor=key==current?Theme.NavActive:Theme.Navy,Font=Theme.Font(9),TextAlign=AppSession.Persian?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft,Cursor=Cursors.Hand,Margin=new Padding(2,2,2,2),AccessibleName=Localization.T(key)};
     button.FlatAppearance.BorderSize=0;button.FlatAppearance.MouseOverBackColor=Theme.NavHover;
     button.Click+=(sender,args)=>Navigate((string)((Button)sender).Tag);
     navigationList98.Controls.Add(button);
    }
   }
   ResizeNavigationItems98();navigationList98.ResumeLayout(true);
  }
  void ResizeNavigationItems98(){
   if(navigationList98==null||navigationList98.IsDisposed)return;
   int width=Math.Max(100,navigationList98.ClientSize.Width-navigationList98.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-10);
   foreach(Control c in navigationList98.Controls)c.Width=width;
  }
  void RefreshNavigationSelection98(){
   if(navigationList98==null)return;
   foreach(Control c in navigationList98.Controls){var b=c as Button;if(b!=null)b.BackColor=(b.Tag as string)==current?Theme.NavActive:Theme.Navy;}
  }
 }
}
