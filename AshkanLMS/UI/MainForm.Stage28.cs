using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowAnalyticsDashboard() {
   Clear();
   var fa=AppSession.Persian;
   var host=new FlowLayoutPanel { Dock=DockStyle.Fill, AutoScroll=true, WrapContents=true, FlowDirection=FlowDirection.LeftToRight, Padding=new Padding(8), BackColor=Color.Transparent };
   content.Controls.Add(host);
   int enrolled=data.Enrollments.Count;
   int present=data.Attendance.Count(x=>string.Equals(x.StatusKey,"Present",StringComparison.OrdinalIgnoreCase));
   int attendance=data.Attendance.Count==0?0:(int)Math.Round(100.0*present/data.Attendance.Count);
   int avgProgress=data.Students.Count==0?0:(int)Math.Round(data.Students.Average(x=>x.Progress));
   decimal avgGrade=data.Grades.Count==0?0:data.Grades.Average(x=>x.Score);
   AddMetric(host,fa?"دانشجویان":"Students",data.Students.Count.ToString(),Theme.Blue);
   AddMetric(host,fa?"دوره‌ها":"Courses",data.Courses.Count.ToString(),Theme.Green);
   AddMetric(host,fa?"ثبت‌نام‌ها":"Enrollments",enrolled.ToString(),Theme.Blue);
   AddMetric(host,fa?"میانگین پیشرفت":"Avg. progress",avgProgress+"%",Theme.Green);
   AddMetric(host,fa?"نرخ حضور":"Attendance",attendance+"%",Theme.Blue);
   AddMetric(host,fa?"میانگین نمرات":"Average grade",avgGrade.ToString("0.0")+" / 20",Theme.Green);
   var chart=AnalyticsCard( fa?"پیشرفت دانشجویان":"Student progress", 485,310);
   var top=data.Students.OrderByDescending(x=>x.Progress).Take(7).ToList();
   var plot=new Panel { Dock=DockStyle.Fill, BackColor=Theme.Card, Padding=new Padding(16) };
   plot.Paint+=(sender,e)=>{
    e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    using(var titleBrush=new SolidBrush(Theme.Muted))
    using(var barBrush=new SolidBrush(Theme.Blue))
    using(var trackBrush=new SolidBrush(Color.FromArgb(226,233,241))) {
     if(top.Count==0){e.Graphics.DrawString(fa?"هنوز دانشجویی ثبت نشده":"No student records yet",Theme.Font(10),titleBrush,18,35);return;}
     int width=Math.Max(30,plot.ClientSize.Width-190);
     for(int i=0;i<top.Count;i++){
      int y=15+i*35; var st=top[i];
      e.Graphics.DrawString(st.Name??"",Theme.Font(9),titleBrush,new RectangleF(12,y,125,26));
      e.Graphics.FillRectangle(trackBrush,140,y+5,width,13);
      e.Graphics.FillRectangle(barBrush,140,y+5,width*Math.Max(0,Math.Min(100,st.Progress))/100f,13);
      e.Graphics.DrawString(st.Progress+"%",Theme.Font(9),titleBrush,145+width,y);
     }
    }
   };
   chart.Controls.Add(plot);host.Controls.Add(chart);
   var info=AnalyticsCard(fa?"شاخص‌های آموزشی":"Academic insights",485,310);
   var notes=new Label { Dock=DockStyle.Fill, Padding=new Padding(18), Font=Theme.Font(10), ForeColor=Theme.Muted,
    Text=(fa?"آزمون‌های پیش‌رو: ":"Upcoming exams: ")+data.Exams.Count(x=>x.Date>=DateTime.Today)+Environment.NewLine+Environment.NewLine+
    (fa?"تکالیف ثبت‌شده: ":"Assignments: ")+data.Assignments.Count+Environment.NewLine+Environment.NewLine+
    (fa?"گواهینامه‌های صادرشده: ":"Certificates issued: ")+data.Certificates.Count+Environment.NewLine+Environment.NewLine+
    (fa?"میانگین نمره از ۲۰ محاسبه شده است.":"Grade average uses the 0–20 scale.")};
   info.Controls.Add(notes);host.Controls.Add(info);
   voidLayoutDashboard(host);
   host.SizeChanged+=(sender,e)=>voidLayoutDashboard(host);
  }
  private Panel AnalyticsCard(string title,int width,int height){
   var panel=new Panel { Width=width,Height=height,BackColor=Theme.Card,Margin=new Padding(8),Padding=new Padding(12) };
   var heading=new Label { Dock=DockStyle.Top,Height=42,Text=title,Font=Theme.Font(12,FontStyle.Bold),ForeColor=Theme.Navy,TextAlign=AppSession.Persian?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft };
   panel.Controls.Add(heading);return panel;
  }
  private void AddMetric(FlowLayoutPanel host,string label,string value,Color accent){
   var card=new Panel { Width=232,Height=114,BackColor=Theme.Card,Margin=new Padding(8) };
   var name=new Label { Text=label,Location=new Point(15,14),Size=new Size(202,30),ForeColor=Theme.Muted,Font=Theme.Font(9,FontStyle.Bold) };
   var metric=new Label { Text=value,Location=new Point(15,47),Size=new Size(202,55),ForeColor=accent,Font=Theme.Font(23,FontStyle.Bold) };
   card.Controls.Add(name);card.Controls.Add(metric);host.Controls.Add(card);
  }
  private void voidLayoutDashboard(FlowLayoutPanel host){
   if(host.IsDisposed)return;
   int available=Math.Max(320,host.ClientSize.Width-35);
   int cardWidth=Math.Max(170,(available/3)-18);
   foreach(Control c in host.Controls){
    if(c.Height==114)c.Width=cardWidth;
    else c.Width=Math.Max(300,(available/2)-18);
   }
  }
 }
}
