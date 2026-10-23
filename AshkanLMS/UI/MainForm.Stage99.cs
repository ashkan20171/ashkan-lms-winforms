using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Core;
namespace AshkanLMS.UI {
 public partial class MainForm {
  void ShowAppearanceSettings99() {
   Clear();
   var card=Card(24,24,Math.Max(640,Math.Min(950,content.ClientSize.Width-80)),350);
   var title=Theme.Label(AppSession.Persian?"ظاهر و رنگ‌بندی":"Appearance & theme",16,FontStyle.Bold);
   title.Location=new Point(24,20);title.AutoSize=false;title.Size=new Size(600,40);card.Controls.Add(title);
   var help=Theme.Label(AppSession.Persian?"تم روشن یا تاریک را انتخاب کنید. تغییرات بلافاصله اعمال و روی این رایانه ذخیره می‌شود.":"Choose light or dark mode. Changes apply immediately and are saved on this computer.",10);
   help.Location=new Point(24,76);help.AutoSize=false;help.Size=new Size(580,60);card.Controls.Add(help);
   var dark=new RadioButton{Text=AppSession.Persian?"تاریک":"Dark",Checked=Theme.DarkMode,Location=new Point(24,145),Width=180,ForeColor=Theme.Text,Font=Theme.Font(11)};
   var light=new RadioButton{Text=AppSession.Persian?"روشن":"Light",Checked=!Theme.DarkMode,Location=new Point(220,145),Width=180,ForeColor=Theme.Text,Font=Theme.Font(11)};
   card.Controls.Add(dark);card.Controls.Add(light);
   var apply=Theme.Button(AppSession.Persian?"اعمال تم":"Apply theme");apply.Location=new Point(24,215);apply.Width=180;
   apply.Click+=(sender,args)=>{
    try { AppearancePreferencesService.SaveDarkMode(dark.Checked);Theme.SetDarkMode(dark.Checked);BuildShell();Navigate("Appearance"); }
    catch(Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException) {MessageBox.Show(AppSession.Persian?"ذخیره تنظیمات ممکن نیست.":"Unable to save appearance preferences.");}
   };card.Controls.Add(apply);
  }
 }
}
