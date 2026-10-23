using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public sealed class RevenueChart : Control {
  private List<MonthlyRevenue> points = new List<MonthlyRevenue>();
  public bool Persian {get;set;}
  public void SetData(IEnumerable<MonthlyRevenue> values) {points=values==null?new List<MonthlyRevenue>():values.ToList();Invalidate();}
  public RevenueChart(){DoubleBuffered=true;BackColor=Color.White;MinimumSize=new Size(240,150);}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e); var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   var area=new Rectangle(48,20,Math.Max(1,Width-65),Math.Max(1,Height-65));
   using(var pen=new Pen(Color.FromArgb(205,216,229),1)){g.DrawLine(pen,area.Left,area.Top,area.Left,area.Bottom);g.DrawLine(pen,area.Left,area.Bottom,area.Right,area.Bottom);}
   if(points.Count==0)return;
   var max=Math.Max(1m,points.Max(p=>p.Amount));
   int slot=Math.Max(1,area.Width/points.Count);
   using(var brush=new SolidBrush(Color.FromArgb(41,121,194)))using(var label=new Font("Segoe UI",8)){
    for(int i=0;i<points.Count;i++){
     var value=Math.Max(0m,points[i].Amount);int bar=(int)((decimal)Math.Max(0,area.Height-8)*value/max);
     int x=area.Left+i*slot+Math.Max(2,slot/5), w=Math.Max(2,slot-Math.Max(4,slot*2/5));
     g.FillRectangle(brush,x,area.Bottom-bar,w,bar);
     string month=points[i].Year.ToString()+"/"+points[i].Month.ToString("00");
     g.DrawString(month,label,Brushes.DimGray,x,area.Bottom+5);
    }
    g.DrawString(max.ToString("N0"),label,Brushes.DimGray,2,area.Top);
   }
  }
 }
}
