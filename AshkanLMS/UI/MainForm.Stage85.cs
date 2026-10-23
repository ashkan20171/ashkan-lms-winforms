using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowFinanceReports(){
   Clear();
   if(AppSession.Role!="Administrator"){content.Controls.Add(new Label{Text=AppSession.Persian?"فقط مدیر به گزارش مالی دسترسی دارد":"Administrator access required",AutoSize=true,Location=new Point(25,25)});return;}
   var card=Card(24,24,980,650);card.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
   card.Controls.Add(Header(AppSession.Persian?"گزارش مالی و بدهکاران":"Revenue & outstanding invoices"));
   var currency=new ComboBox{Location=new Point(22,58),Width=120,DropDownStyle=ComboBoxStyle.DropDownList};currency.Items.AddRange(new object[]{"IRR","EUR"});currency.SelectedIndex=0;card.Controls.Add(currency);
   var heading=new Label{Location=new Point(160,60),Width=750,Height=35,Font=Theme.Font(10,FontStyle.Bold)};card.Controls.Add(heading);
   var trend=new RevenueChart{Persian=AppSession.Persian,Location=new Point(22,106),Size=new Size(450,200),Anchor=AnchorStyles.Top|AnchorStyles.Left};card.Controls.Add(trend);
   var debtors=Grid();debtors.Location=new Point(22,345);debtors.Size=new Size(930,240);debtors.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom;card.Controls.Add(debtors);
   var debtTitle=new Label{Location=new Point(22,316),AutoSize=true,Text=AppSession.Persian?"صورتحساب‌های تسویه‌نشده":"Outstanding invoices",Font=Theme.Font(11,FontStyle.Bold)};card.Controls.Add(debtTitle);
   var invoiceId=new NumericUpDown{Location=new Point(510,115),Width=115,Minimum=1,Maximum=100000000};card.Controls.Add(invoiceId);
   var print=Theme.Button(AppSession.Persian?"چاپ صورتحساب / رسید":"Print invoice / receipt");print.Location=new Point(640,112);print.Width=240;card.Controls.Add(print);
   var branding=Theme.Button(AppSession.Persian?"تنظیمات چاپ":"Print settings");branding.Location=new Point(640,226);branding.Width=240;card.Controls.Add(branding);branding.Click+=(s,e)=>EditInvoiceBranding();
   var help=new Label{Location=new Point(510,155),Width=400,Height=60,Text=AppSession.Persian?"شناسه صورتحساب را وارد کنید. چاپ با پیش‌نمایش انجام می‌شود.":"Enter an invoice ID. Preview before printing."};card.Controls.Add(help);
   Action refresh=()=>{
    var c=currency.Text;
    var monthly=TuitionReportingService.MonthlyPayments(data.TuitionInvoices,data.TuitionPayments,c,DateTime.Now);
    trend.SetData(monthly);
    var debts=TuitionReportingService.Debtors(data.TuitionInvoices,data.TuitionPayments,c,DateTime.Now);
    debtors.DataSource=debts.Select(x=>new {x.InvoiceId,x.StudentId,x.Outstanding,x.DueDate}).ToList();
    heading.Text=(AppSession.Persian?"مجموع بدهی":"Outstanding total")+": "+debts.Sum(x=>x.Outstanding).ToString("N2")+" "+c;
   };
   currency.SelectedIndexChanged+=(sender,args)=>refresh();
   print.Click+=(sender,args)=>{
    var invoice=data.TuitionInvoices.FirstOrDefault(i=>i.Id==(int)invoiceId.Value);
    if(invoice==null){MessageBox.Show(AppSession.Persian?"صورتحساب پیدا نشد":"Invoice not found");return;}
    var payments=data.TuitionPayments.Where(p=>p.InvoiceId==invoice.Id).OrderBy(p=>p.PaidAt).ToList();
    var lines=InvoicePrintService.BuildLines(invoice,payments,AppSession.Persian);
    var brand=InvoiceBrandingService.Load();
    using(var document=new PrintDocument()){
     document.DocumentName="AshkanLMS Invoice "+invoice.Id;
     int nextLine=0;
     document.BeginPrint+=(ps,pe)=>{nextLine=0;};
     document.PrintPage+=(ps,pe)=>{
      using(var font=new Font("Tahoma",10))
      using(var headingFont=new Font("Tahoma",15,FontStyle.Bold))
      using(var format=new StringFormat()){
       bool fa=AppSession.Persian;
       format.Alignment=fa?StringAlignment.Far:StringAlignment.Near;
       if(fa)format.FormatFlags|=StringFormatFlags.DirectionRightToLeft;
       var bounds=pe.MarginBounds;
       float y=bounds.Top;
       var brandName=InvoiceBrandingService.Sanitize(brand.AcademyName);
       pe.Graphics.DrawString(brandName+" | "+(fa?"صورتحساب شهریه":"Tuition invoice"),headingFont,Brushes.Black,new RectangleF(bounds.Left,y,bounds.Width-85,40),format);
       if(!string.IsNullOrWhiteSpace(brand.LogoPath) && System.IO.File.Exists(brand.LogoPath)){
        try{using(var logo=Image.FromFile(brand.LogoPath))pe.Graphics.DrawImage(logo,new Rectangle(bounds.Right-75,y==0?bounds.Top:(int)y,70,55));}catch(ArgumentException){}catch(System.IO.IOException){}catch(UnauthorizedAccessException){}
       }
       y+=47;
       using(var smallFont=new Font("Tahoma",8)){
        var info=InvoiceBrandingService.Sanitize(brand.Address)+"  "+InvoiceBrandingService.Sanitize(brand.Phone)+"  "+InvoiceBrandingService.Sanitize(brand.Website);
        pe.Graphics.DrawString(info,smallFont,Brushes.Gray,new RectangleF(bounds.Left,y,bounds.Width,28),format);
       }
       y+=35;
       float lineHeight=font.GetHeight(pe.Graphics)+11;
       float footerHeight=55;
       while(nextLine<lines.Count && y+lineHeight<=bounds.Bottom-footerHeight){
        pe.Graphics.DrawString(lines[nextLine],font,Brushes.Black,new RectangleF(bounds.Left,y,bounds.Width,lineHeight),format);
        nextLine++;
        y+=lineHeight;
       }
       int page=InvoicePrintService.PageNumber(lines.Count,nextLine,Math.Max(1,(int)((bounds.Height-85)/lineHeight)));
       pe.Graphics.DrawString(InvoiceBrandingService.Sanitize(brand.Footer),font,Brushes.Gray,new RectangleF(bounds.Left,bounds.Bottom-47,bounds.Width,22),format);
       pe.Graphics.DrawString((fa?"صفحه ":"Page ")+page,font,Brushes.Gray,new RectangleF(bounds.Left,bounds.Bottom-25,bounds.Width,25),format);
       pe.HasMorePages=nextLine<lines.Count;
      }
     };
     using(var preview=new PrintPreviewDialog{Document=document,Width=960,Height=700})preview.ShowDialog(this);
    }
   };
   refresh();
  }
 }
}
