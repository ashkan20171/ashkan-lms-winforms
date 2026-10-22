using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowUserManagement(){
   Clear();if(AppSession.Role!="Administrator"){content.Controls.Add(new Label{Text="Administrator access required",AutoSize=true,Location=new Point(25,25)});return;}
   var auth=new AuthenticationService();var card=Card(24,24,990,580);
   card.Controls.Add(new Label{Text=AppSession.Persian?"مدیریت حساب‌های کاربری":"User account management",Location=new Point(20,20),AutoSize=true,Font=Theme.Font(15,FontStyle.Bold)});
   var grid=Grid();grid.Location=new Point(20,70);grid.Size=new Size(940,340);card.Controls.Add(grid);
   Action refresh=()=>{grid.DataSource=auth.ListAccounts(AppSession.Role).Select(a=>new {a.Username,a.Role,Status=a.Disabled?"Disabled":"Active"}).ToList();};
   Func<string,string> prompt=(title)=>{using(var f=new Form{Text=title,Size=new Size(390,170),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog}){
    var field=new TextBox{Left=20,Top=22,Width=330,UseSystemPasswordChar=title.ToLowerInvariant().Contains("password")};var ok=new Button{Text="OK",Left=250,Top=65,Width=100,DialogResult=DialogResult.OK};f.Controls.Add(field);f.Controls.Add(ok);f.AcceptButton=ok;return f.ShowDialog(this)==DialogResult.OK?field.Text:null;}};
   Action<Action> execute=action=>{try{action();refresh();}catch(Exception ex){MessageBox.Show(ex.Message,"AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
   Func<string> selected=()=>grid.CurrentRow==null?null:Convert.ToString(grid.CurrentRow.Cells["Username"].Value);
   var create=new Button{Text=AppSession.Persian?"ایجاد حساب":"Create account",Location=new Point(20,435),Size=new Size(170,38)};
   create.Click+=(s,e)=>{string username=prompt("Username");if(username==null)return;string role=prompt("Role: Administrator / Instructor / Student");if(role==null)return;string password=prompt("New password (12+ characters)");if(password==null)return;execute(()=>auth.CreateAccount(AppSession.Role,username,role,password));};
   var reset=new Button{Text=AppSession.Persian?"بازنشانی رمز":"Reset password",Location=new Point(205,435),Size=new Size(170,38)};
   reset.Click+=(s,e)=>{string username=selected();if(username==null)return;string password=prompt("New password (12+ characters)");if(password!=null)execute(()=>auth.ResetPassword(AppSession.Role,username,password));};
   var toggle=new Button{Text=AppSession.Persian?"فعال / غیرفعال":"Enable / Disable",Location=new Point(390,435),Size=new Size(170,38)};
   toggle.Click+=(s,e)=>{string username=selected();if(username==null)return;bool enabled=Convert.ToString(grid.CurrentRow.Cells["Status"].Value)=="Disabled";if(MessageBox.Show("Change account status for "+username+"?","Confirm",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;execute(()=>auth.SetAccountEnabled(AppSession.Role,AppSession.UserName,username,enabled));};
   card.Controls.Add(create);card.Controls.Add(reset);card.Controls.Add(toggle);refresh();
  }
 }
}
