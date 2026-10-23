using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI {
 public partial class MainForm {
  void ShowAcademicManagement(string key) {
   Clear(); var card=Card(24,24,986,570);
   card.Controls.Add(Header(Localization.T(key)));
   var grid=Grid(); grid.SetBounds(18,105,950,420); card.Controls.Add(grid);
   if(key=="Exams") grid.DataSource=data.Exams.Select(x=>new{x.Id,x.Title,x.Course,x.Date,x.Questions,x.StatusKey}).ToList();
   else if(key=="Assignments") grid.DataSource=data.Assignments.Select(x=>new{x.Id,x.Title,x.Course,x.DueDate,x.Submitted,x.StatusKey}).ToList();
   else if(key=="Attendance") grid.DataSource=data.Attendance.Select(x=>new{x.Id,x.Student,x.Course,x.Date,x.StatusKey}).ToList();
   else if(key=="Grades") grid.DataSource=data.Grades.Select(x=>new{x.Id,x.Student,x.Course,x.Score,x.Letter}).ToList();
   else grid.DataSource=data.Certificates.Select(x=>new{x.Id,x.Student,x.Course,x.IssuedAt,x.Code}).ToList();
   var add=Theme.Button(AppSession.Persian?"ثبت مورد جدید":"Add new record");add.SetBounds(18,56,200,38);card.Controls.Add(add);
   add.Click+=(sender,args)=>OpenAcademicDialog(key);
   var export=Theme.Button(AppSession.Persian?"خروجی CSV":"Export CSV");export.SetBounds(230,56,175,38);card.Controls.Add(export);
   export.Click+=(sender,args)=>{using(var dlg=new SaveFileDialog{Filter="CSV UTF-8 (*.csv)|*.csv",FileName="AshkanLMS_"+key+".csv"}){
    if(dlg.ShowDialog(this)==DialogResult.OK)try{AcademicExportService.ExportGrid(grid,dlg.FileName);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Export error");}}};
   var excel=Theme.Button(AppSession.Persian?"خروجی Excel":"Export Excel");excel.SetBounds(635,56,175,38);card.Controls.Add(excel);
   excel.Click+=(sender,args)=>{using(var dlg=new SaveFileDialog{Filter="Excel XML (*.xml)|*.xml",FileName="AshkanLMS_"+key+".xml"}){
    if(dlg.ShowDialog(this)==DialogResult.OK)try{AcademicExportService.ExportExcelXml(grid,dlg.FileName);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Export error");}}};
   if(key=="Certificates"){
    var print=Theme.Button(AppSession.Persian?"چاپ گواهینامه":"Print certificate");print.SetBounds(417,56,205,38);card.Controls.Add(print);
    print.Click+=(sender,args)=>{if(grid.CurrentRow==null){MessageBox.Show(this,AppSession.Persian?"یک گواهینامه انتخاب کنید.":"Select a certificate.");return;}
     int id=Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);var cert=data.Certificates.FirstOrDefault(x=>x.Id==id);if(cert==null)return;
     using(var dlg=new SaveFileDialog{Filter="Printable HTML (*.html)|*.html",FileName="Certificate_"+cert.Code+".html"}){
      if(dlg.ShowDialog(this)==DialogResult.OK)try{AcademicExportService.ExportCertificate(cert,dlg.FileName);System.Diagnostics.Process.Start(dlg.FileName);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Certificate export error");}}};
   }

  }
  void OpenAcademicDialog(string key) {
   using(var dialog=new Form{Text=Localization.T(key),StartPosition=FormStartPosition.CenterParent,Size=new Size(490,440),Font=Theme.Font(10),BackColor=Theme.Canvas,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
    var courses=new ComboBox{Left=24,Top=62,Width=425,DropDownStyle=ComboBoxStyle.DropDownList};
    courses.DisplayMember="Title"; courses.ValueMember="Id"; courses.DataSource=data.Courses.ToList();
    var students=new ComboBox{Left=24,Top=128,Width=425,DropDownStyle=ComboBoxStyle.DropDownList};
    students.DisplayMember="Name"; students.ValueMember="Id"; students.DataSource=data.Students.ToList();
    var title=new TextBox{Left=24,Top=128,Width=425};
    var date=new DateTimePicker{Left=24,Top=194,Width=425,Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd HH:mm"};
    var number=new NumericUpDown{Left=24,Top=260,Width=200,Minimum=key=="Exams"?1:0,Maximum=key=="Exams"?500:20,DecimalPlaces=key=="Grades"?2:0};
    var present=new CheckBox{Left=24,Top=260,Width=350,Text=AppSession.Persian?"حاضر":"Present",Checked=true};
    dialog.Controls.Add(new Label{Left=24,Top=37,AutoSize=true,Text=AppSession.Persian?"دوره":"Course"});dialog.Controls.Add(courses);
    bool person=key=="Attendance"||key=="Grades"||key=="Certificates";
    dialog.Controls.Add(new Label{Left=24,Top=103,AutoSize=true,Text=person?(AppSession.Persian?"دانشجو":"Student"):(AppSession.Persian?"عنوان":"Title")});
    dialog.Controls.Add(person?(Control)students:title);
    if(key=="Exams"||key=="Assignments"||key=="Attendance") {dialog.Controls.Add(new Label{Left=24,Top=169,AutoSize=true,Text=AppSession.Persian?"تاریخ":"Date"});dialog.Controls.Add(date);}
    if(key=="Exams"||key=="Grades") {dialog.Controls.Add(new Label{Left=24,Top=235,AutoSize=true,Text=key=="Exams"?"Questions / سوالات":"Score / نمره"});dialog.Controls.Add(number);}
    if(key=="Attendance")dialog.Controls.Add(present);
    var save=Theme.Button(AppSession.Persian?"ذخیره":"Save");save.SetBounds(290,326,159,38);dialog.Controls.Add(save);
    save.Click+=(sender,args)=>{
     try {
      if(courses.SelectedItem==null)throw new InvalidOperationException(AppSession.Persian?"ابتدا دوره را انتخاب کنید.":"Select a course.");
      int courseId=(int)courses.SelectedValue; string course=data.Courses.First(x=>x.Id==courseId).Title; int studentId=students.SelectedValue is int?(int)students.SelectedValue:0;
      if(person&&studentId==0)throw new InvalidOperationException(AppSession.Persian?"دانشجو را انتخاب کنید.":"Select a student.");
      var ops=new AcademicOperations(data);
      if(key=="Exams")ops.ScheduleExam(title.Text,course,date.Value,(int)number.Value);
      else if(key=="Assignments")ops.CreateAssignment(title.Text,course,date.Value);
      else if(key=="Attendance")ops.RecordAttendance(studentId,courseId,date.Value,present.Checked);
      else if(key=="Grades")ops.RecordGrade(studentId,courseId,number.Value);
      else ops.IssueCertificate(studentId,courseId);
      dialog.DialogResult=DialogResult.OK;
     } catch(Exception ex){MessageBox.Show(dialog,ex.Message,AppSession.Persian?"خطای اعتبارسنجی":"Validation error",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    };
    if(courses.Items.Count>0)courses.SelectedIndex=0;
    if(students.Items.Count>0)students.SelectedIndex=0;
    if(dialog.ShowDialog(this)==DialogResult.OK)ShowAcademicManagement(key);
   }
  }
 }
}
