using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public sealed class SecurityTrendChart:Control {
  private int[] counts=new int[7];
  public SecurityTrendChart(){DoubleBuffered=true;BackColor=Color.FromArgb(247,250,255);}
  public void SetEvents(IEnumerable<SecurityAuditService.Entry> events){
   var today=DateTime.Today;var data=new int[7];
   foreach(var e in events??Enumerable.Empty<SecurityAuditService.Entry>()){
    if(e==null)continue;var diff=(today-e.UtcTime.ToLocalTime().Date).Days;
    if(diff>=0&&diff<7)data[6-diff]++;
   }counts=data;Invalidate();
  }
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var brush=new SolidBrush(Color.FromArgb(52,107,193)))
   using(var textBrush=new SolidBrush(Color.FromArgb(70,84,105)))
   using(var font=new Font(Font.FontFamily,8f)){
    int max=Math.Max(1,counts.Max());float slot=(Width-24f)/7f;
    for(int i=0;i<7;i++){
     float height=(Height-37f)*counts[i]/max;
     g.FillRectangle(brush,16+i*slot,Height-25-height,Math.Max(6,slot-14),height);
     var day=DateTime.Today.AddDays(i-6).ToString("MM/dd");
     g.DrawString(day,font,textBrush,14+i*slot,Height-19);
    }
   }
  }
 }
}
