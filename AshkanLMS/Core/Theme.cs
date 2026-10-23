using System; using System.Drawing; using System.IO; using System.Windows.Forms;
namespace AshkanLMS.Core { public static class Theme { public static Color Navy=Color.FromArgb(22,34,53), Blue=Color.FromArgb(48,104,214), Sky=Color.FromArgb(234,241,255), Green=Color.FromArgb(38,166,91), Amber=Color.FromArgb(245,166,35), Red=Color.FromArgb(224,76,76), Surface=Color.FromArgb(248,250,254), Canvas=Color.FromArgb(229,237,248), Text=Color.FromArgb(36,44,58), Muted=Color.FromArgb(96,108,126); public static Color Header=Color.FromArgb(238,244,252), Card=Color.FromArgb(248,250,254), NavHover=Color.FromArgb(31,48,72), NavActive=Color.FromArgb(48,104,214); public static bool DarkMode { get; private set; }
 public static void SetDarkMode(bool enabled) {
  DarkMode=enabled;
  Navy=enabled?Color.FromArgb(13,21,35):Color.FromArgb(22,34,53);
  Blue=enabled?Color.FromArgb(90,153,255):Color.FromArgb(48,104,214);
  Sky=enabled?Color.FromArgb(33,47,68):Color.FromArgb(234,241,255);
  Surface=enabled?Color.FromArgb(30,41,57):Color.FromArgb(248,250,254);
  Canvas=enabled?Color.FromArgb(19,29,44):Color.FromArgb(229,237,248);
  Text=enabled?Color.FromArgb(238,244,252):Color.FromArgb(36,44,58);
  Muted=enabled?Color.FromArgb(176,193,215):Color.FromArgb(96,108,126);
  Header=enabled?Color.FromArgb(30,41,57):Color.FromArgb(238,244,252);
  Card=enabled?Color.FromArgb(35,49,67):Color.FromArgb(248,250,254);
  NavHover=enabled?Color.FromArgb(41,61,89):Color.FromArgb(31,48,72);
  NavActive=enabled?Color.FromArgb(50,95,163):Color.FromArgb(48,104,214);
 }
 public static string BackgroundImagePath { get { return BackgroundManager.CurrentPath; } } public static Image LoadBackgroundImage(){return BackgroundManager.LoadCurrent();} public static Font Font(float size=10, FontStyle style=FontStyle.Regular)=>new Font("Segoe UI",size,style); public static Button Button(string text){ return new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=Blue,ForeColor=Color.White,Font=Font(10,FontStyle.Bold),Height=38,Cursor=Cursors.Hand,FlatAppearance={BorderSize=0}}; } public static Label Label(string text,float size=10,FontStyle style=FontStyle.Regular){return new Label{Text=text,AutoSize=true,Font=Font(size,style),ForeColor=Text};} } }