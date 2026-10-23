using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void EditInvoiceBranding(){
   if(AppSession.Role!="Administrator"){MessageBox.Show(AppSession.Persian?"دسترسی فقط برای مدیر":"Administrator access required");return;}
   var settings=InvoiceBrandingService.Load();
   using(var dialog=new Form{Text=AppSession.Persian?"تنظیمات سربرگ صورتحساب":"Invoice branding settings",Width=680,Height=490,StartPosition=FormStartPosition.CenterParent,RightToLeft=AppSession.Persian?RightToLeft.Yes:RightToLeft.No,RightToLeftLayout=AppSession.Persian}){
    var labels=new[]{AppSession.Persian?"نام آموزشگاه":"Academy name",AppSession.Persian?"نشانی":"Address",AppSession.Persian?"تلفن":"Phone",AppSession.Persian?"وب‌سایت":"Website",AppSession.Persian?"مسیر فایل لوگو":"Logo file path",AppSession.Persian?"متن پابرگ":"Footer"};
    var values=new[]{settings.AcademyName,settings.Address,settings.Phone,settings.Website,settings.LogoPath,settings.Footer};
    var inputs=new TextBox[6];
    for(int i=0;i<6;i++){var y=20+i*59;dialog.Controls.Add(new Label{Text=labels[i],Location=new Point(20,y),Size=new Size(175,25)});inputs[i]=new TextBox{Location=new Point(205,y),Size=new Size(425,27),Text=values[i]??""};dialog.Controls.Add(inputs[i]);}
    var browse=new Button{Text=AppSession.Persian?"انتخاب لوگو":"Browse logo",Location=new Point(205,375),Width=125};dialog.Controls.Add(browse);
    browse.Click+=(s,e)=>{using(var picker=new OpenFileDialog{Filter="Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"})if(picker.ShowDialog(dialog)==DialogResult.OK)inputs[4].Text=picker.FileName;};
    var save=new Button{Text=AppSession.Persian?"ذخیره":"Save",Location=new Point(505,375),Width=125};dialog.Controls.Add(save);
    save.Click+=(s,e)=>{try{InvoiceBrandingService.Save(new InvoiceBranding{AcademyName=inputs[0].Text,Address=inputs[1].Text,Phone=inputs[2].Text,Website=inputs[3].Text,LogoPath=inputs[4].Text,Footer=inputs[5].Text});dialog.DialogResult=DialogResult.OK;dialog.Close();}catch(Exception ex){MessageBox.Show(ex.Message,"AshkanLMS");}};
    dialog.ShowDialog(this);
   }
  }
 }
}
