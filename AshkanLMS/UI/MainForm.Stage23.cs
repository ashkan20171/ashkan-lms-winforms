using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowPasswordChange(){
   using(var dialog=new Form {Text=AppSession.Persian?"تغییر رمز عبور":"Change password",Size=new Size(420,310),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Theme.Font()}){
    var oldBox=new TextBox{Location=new Point(30,45),Width=340,UseSystemPasswordChar=true};
    var newBox=new TextBox{Location=new Point(30,110),Width=340,UseSystemPasswordChar=true};
    var confirm=new TextBox{Location=new Point(30,175),Width=340,UseSystemPasswordChar=true};
    dialog.Controls.Add(new Label{Text=AppSession.Persian?"رمز فعلی":"Current password",Location=new Point(30,20),AutoSize=true});
    dialog.Controls.Add(new Label{Text=AppSession.Persian?"رمز جدید (حداقل ۱۲ کاراکتر)":"New password (12+ characters)",Location=new Point(30,85),AutoSize=true});
    dialog.Controls.Add(new Label{Text=AppSession.Persian?"تکرار رمز جدید":"Confirm new password",Location=new Point(30,150),AutoSize=true});
    dialog.Controls.Add(oldBox);dialog.Controls.Add(newBox);dialog.Controls.Add(confirm);
    var save=new Button{Text=AppSession.Persian?"ذخیره":"Save",Location=new Point(270,215),Width=100};dialog.Controls.Add(save);
    save.Click+=(s,e)=>{try{if(newBox.Text!=confirm.Text)throw new ArgumentException("Passwords do not match.");new AuthenticationService().ChangePassword(AppSession.UserName,oldBox.Text,newBox.Text);MessageBox.Show(AppSession.Persian?"رمز عبور تغییر کرد":"Password updated");dialog.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(ex.Message,"AshkanLMS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
    dialog.ShowDialog(this);
   }
  }
 }
}
