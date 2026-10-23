using System;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
namespace AshkanLMS.UI {
 public partial class MainForm {
  private void ShowReminderEmailDialog(InstallmentReminder item){
   if(AppSession.Role!="Administrator")return;
   bool fa=AppSession.Persian;
   using(var dialog=new Form{Text=fa?"پیش‌نمایش و ارسال ایمیل":"Preview and send reminder",Width=690,Height=680,StartPosition=FormStartPosition.CenterParent,RightToLeft=fa?RightToLeft.Yes:RightToLeft.No}){
    var host=new TextBox{Text="",Width=420};var port=new NumericUpDown{Minimum=1,Maximum=65535,Value=587,Width=420};
    var username=new TextBox{Width=420};var password=new TextBox{Width=420,UseSystemPasswordChar=true};
    var sender=new TextBox{Width=420};var recipient=new TextBox{Width=420};
    var subject=new TextBox{Text=fa?"یادآوری سررسید شهریه":"Tuition installment reminder",Width=420};
    var body=new TextBox{Text=InstallmentReminderService.Draft(item,fa),Multiline=true,ScrollBars=ScrollBars.Vertical,Width=420,Height=145};
    var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=11,AutoScroll=true};
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,185));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
    int nextRow=0;Action<string,Control> add=(label,control)=>{int index=nextRow++;layout.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(3,9,3,4)},0,index);layout.Controls.Add(control,1,index);layout.RowCount++;};
    add("SMTP host",host);add("SMTP port",port);add("SMTP username",username);add("SMTP password (session only)",password);add("From",sender);add("To",recipient);add(fa?"موضوع":"Subject",subject);add(fa?"متن ایمیل":"Message",body);
    var notice=new Label{AutoSize=true,MaximumSize=new Size(590,0),Text=fa?"ارسال فقط پس از تأیید شما انجام می‌شود. گذرواژه ذخیره نمی‌شود. TLS الزامی است. ثبت SMTP به معنی تحویل به صندوق گیرنده نیست.":"Email is sent only after confirmation. Password is never saved. TLS is required. SMTP acceptance does not guarantee inbox delivery."};
    layout.Controls.Add(notice,0,8);layout.SetColumnSpan(notice,2);
    var send=Theme.Button(fa?"تأیید و ارسال":"Confirm and send");send.Width=180;
    send.Click+=(s,e)=>{
     var request=new ReminderEmailRequest{Host=host.Text.Trim(),Port=(int)port.Value,Username=username.Text,Password=password.Text,Sender=sender.Text.Trim(),Recipient=recipient.Text.Trim(),Subject=subject.Text,Body=body.Text,EnableSsl=true};
     try{
      ReminderEmailService.Validate(request);
      if(MessageBox.Show(fa?"ایمیل به آدرس واردشده ارسال شود؟":"Send email to the entered recipient?",dialog.Text,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
      if(ReminderEmailService.WasAccepted(item.Key,request.Recipient)){
       if(MessageBox.Show(fa?"برای این قسط و گیرنده قبلاً ارسال ثبت شده است. دوباره ارسال شود؟":"An SMTP acceptance already exists for this installment and recipient. Send again?",dialog.Text,MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
      }
      send.Enabled=false;try{ReminderEmailService.Send(request);ReminderEmailService.RecordAccepted(item.Key,request.Recipient);MessageBox.Show(fa?"سرور SMTP پیام را پذیرفت؛ تحویل نهایی تأیید نشده است.":"SMTP accepted the message; final delivery is not confirmed.");dialog.DialogResult=DialogResult.OK;dialog.Close();}catch(SmtpException ex){ReminderEmailService.RecordFailure(item.Key,request.Recipient,"SMTP failure");MessageBox.Show(ex.Message);}finally{send.Enabled=true;}
     }catch(SmtpException ex){MessageBox.Show(ex.Message);}catch(ArgumentException ex){MessageBox.Show(ex.Message);}catch(System.IO.IOException ex){MessageBox.Show(ex.Message);}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message);}catch(InvalidOperationException ex){MessageBox.Show(ex.Message);}
    };
    layout.Controls.Add(send,1,9);dialog.Controls.Add(layout);dialog.ShowDialog(this);
   }
  }
 }
}
