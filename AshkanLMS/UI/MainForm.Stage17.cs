using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI {
 public partial class MainForm {
  void ShowInstructorManagement(){
   Clear();var card=Card(24,24,986,570);card.Controls.Add(Header(Localization.T("Instructors")));
   var grid=Grid();grid.SetBounds(18,145,950,380);
   card.Controls.Add(grid);AttachInstructorSearch103(card,grid);AttachAcademicProfiles104(card,grid,false);
   var add=Theme.Button(AppSession.Persian?"افزودن مدرس":"Add instructor");add.SetBounds(18,55,175,36);add.Click+=(s,e)=>{
    using(var dialog=new Form{Text=add.Text,Size=new Size(420,330),StartPosition=FormStartPosition.CenterParent,Font=Theme.Font(10)}){
     var name=new TextBox{Left=25,Top=45,Width=350};var expertise=new TextBox{Left=25,Top=115,Width=350};var email=new TextBox{Left=25,Top=185,Width=350};
     dialog.Controls.AddRange(new Control[]{new Label{Text="Name / نام",Left=25,Top=22,AutoSize=true},name,new Label{Text="Expertise / تخصص",Left=25,Top=92,AutoSize=true},expertise,new Label{Text="Email",Left=25,Top=162,AutoSize=true},email});
     var save=Theme.Button(AppSession.Persian?"ذخیره":"Save");save.SetBounds(245,225,130,36);dialog.Controls.Add(save);
     save.Click+=(a,b)=>{if(name.Text.Trim().Length<2||expertise.Text.Trim().Length<2||!ValidAcademicEmail102(email.Text)){MessageBox.Show(dialog,AppSession.Persian?"نام، تخصص و ایمیل معتبر وارد کنید.":"Enter a valid name, expertise and email.");return;}try{data.AddInstructor(name.Text.Trim(),expertise.Text.Trim(),email.Text.Trim());dialog.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(dialog,ex.Message);}};
     LayoutDirectionManager.Apply(dialog);if(dialog.ShowDialog(this)==DialogResult.OK)ShowInstructorManagement();
    }
   };card.Controls.Add(add);
   var delete=Theme.Button(AppSession.Persian?"حذف مدرس":"Delete instructor");delete.SetBounds(205,55,175,36);delete.Click+=(s,e)=>{
    if(grid.CurrentRow==null)return;int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
    if(MessageBox.Show(this,AppSession.Persian?"مدرس انتخاب‌شده حذف شود؟":"Delete selected instructor?",AppSession.Persian?"تأیید":"Confirm",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
    try{data.DeleteInstructor(id);ShowInstructorManagement();}catch(Exception ex){MessageBox.Show(this,ex.Message);}
   };card.Controls.Add(delete);FitAcademicCard102(card,grid);
  }
  void ShowEnrollmentManagement(){
   Clear();var card=Card(24,24,986,570);card.Controls.Add(Header(Localization.T("Enrollments")));
   var grid=Grid();grid.SetBounds(18,110,950,415);grid.DataSource=data.Enrollments.Select(x=>new{x.Id,x.Student,x.Course,x.Date,Status=x.StatusKey}).ToList();card.Controls.Add(grid);
   var students=new ComboBox{Left=18,Top=55,Width=270,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id",DataSource=data.Students.ToList()};
   var courses=new ComboBox{Left=300,Top=55,Width=270,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Title",ValueMember="Id",DataSource=data.Courses.ToList()};
   card.Controls.AddRange(new Control[]{students,courses});
   var add=Theme.Button(AppSession.Persian?"ثبت نام":"Enroll");add.SetBounds(585,55,160,36);add.Click+=(s,e)=>{
    if(students.SelectedValue==null||courses.SelectedValue==null){MessageBox.Show(this,AppSession.Persian?"ابتدا دانشجو و دوره را ثبت و انتخاب کنید.":"Create and select a student and a course first.");return;}
    try{data.EnrollStudent(Convert.ToInt32(students.SelectedValue),Convert.ToInt32(courses.SelectedValue));ShowEnrollmentManagement();}catch(Exception ex){MessageBox.Show(this,ex.Message);}
   };card.Controls.Add(add);
   var delete=Theme.Button(AppSession.Persian?"لغو ثبت نام":"Cancel enrollment");delete.SetBounds(760,55,200,36);delete.Click+=(s,e)=>{
    if(grid.CurrentRow==null)return;int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
    if(MessageBox.Show(this,AppSession.Persian?"ثبت‌نام انتخاب‌شده لغو شود؟":"Cancel selected enrollment?",AppSession.Persian?"تأیید":"Confirm",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
    try{data.CancelEnrollment(id);ShowEnrollmentManagement();}catch(Exception ex){MessageBox.Show(this,ex.Message);}
   };card.Controls.Add(delete);FitAcademicCard102(card,grid);
  }
 }
}
