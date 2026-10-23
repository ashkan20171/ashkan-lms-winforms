using System;
using System.Drawing;
using System.Windows.Forms;
namespace AshkanLMS.UI {
 internal static class TuitionVisualStatus {
  internal static string GetStatus(decimal invoiced,decimal paid,DateTime due,DateTime today) {
   if(paid>=invoiced)return "Paid";
   if(due.Date<today.Date)return "Overdue";
   return paid>0?"Partial":"Unpaid";
  }
  internal static void StyleGrid(DataGridView grid,bool persian) {
   if(grid.Columns["Status"]==null)return;
   grid.Columns["Status"].HeaderText=persian?"وضعیت":"Status";
   grid.CellFormatting+=(sender,e)=>{
    if(e.RowIndex<0 || e.ColumnIndex!=grid.Columns["Status"].Index)return;
    var status=Convert.ToString(e.Value);
    e.Value=status=="Paid"?(persian?"تسویه‌شده":"Paid"):
     status=="Overdue"?(persian?"معوق":"Overdue"):
     status=="Partial"?(persian?"پرداخت ناقص":"Partial"):(persian?"پرداخت‌نشده":"Unpaid");
    e.CellStyle.ForeColor=status=="Paid"?Color.FromArgb(22,115,71):status=="Overdue"?Color.FromArgb(181,43,43):status=="Partial"?Color.FromArgb(152,97,20):Color.FromArgb(65,83,103);
    e.CellStyle.Font=new Font(grid.Font,FontStyle.Bold);
    e.FormattingApplied=true;
   };
  }
 }
}
