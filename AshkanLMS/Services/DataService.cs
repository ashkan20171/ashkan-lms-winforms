using System; using System.IO; using System.Runtime.Serialization.Json; using System.Collections.Generic; using System.Linq; using AshkanLMS.Models;
namespace AshkanLMS.Services { public sealed class DataService {
 readonly AuthenticationService authentication = new AuthenticationService();
 readonly SqlStateStore sqlStore = CreateSqlStore();
 static SqlStateStore CreateSqlStore(){
  var setting=System.Configuration.ConfigurationManager.AppSettings["PersistenceMode"];
  if(!string.Equals(setting,"SqlServer",StringComparison.OrdinalIgnoreCase))return null;
  var connection=System.Configuration.ConfigurationManager.ConnectionStrings["AshkanLMS"];
  if(connection==null || string.IsNullOrWhiteSpace(connection.ConnectionString))throw new InvalidOperationException("SQL Server mode requires an AshkanLMS connection string in App.config.");
  return new SqlStateStore(connection.ConnectionString);
 }
 public string FirstRunMessage {get {return authentication.SetupMessage;} }
 public List<Student> Students {get;}=new List<Student>{new Student{Id=1001,Name="Sara Ahmadi",Email="sara@example.com",StatusKey="Active",Progress=82},new Student{Id=1002,Name="Ali Rezaei",Email="ali@example.com",StatusKey="Active",Progress=64},new Student{Id=1003,Name="Maryam Mohammadi",Email="maryam@example.com",StatusKey="Active",Progress=91},new Student{Id=1004,Name="Reza Karimi",Email="reza@example.com",StatusKey="NeedsFollowUp",Progress=38}};
 public List<Course> Courses {get;}=new List<Course>{new Course{Id=1,Title="C# & .NET Fundamentals",Instructor="Ashkan Motaei",Students=42,Progress=78,StatusKey="Active"},new Course{Id=2,Title="Clean Code in Practice",Instructor="Ashkan Motaei",Students=31,Progress=62,StatusKey="Active"},new Course{Id=3,Title="SQL Server Essentials",Instructor="N. Rahimi",Students=28,Progress=84,StatusKey="Active"},new Course{Id=4,Title="UI/UX for Desktop Apps",Instructor="S. Ahmadi",Students=25,Progress=56,StatusKey="Draft"}};
 public List<Instructor> Instructors {get;}=new List<Instructor>{new Instructor{Id=201,Name="Ashkan Motaei",Expertise="C# / .NET / Clean Code",Email="ashkan@demo.local",Courses=2},new Instructor{Id=202,Name="N. Rahimi",Expertise="SQL Server",Email="rahimi@demo.local",Courses=1},new Instructor{Id=203,Name="S. Ahmadi",Expertise="UI/UX",Email="ahmadi@demo.local",Courses=1}};
 public List<Enrollment> Enrollments {get;}=new List<Enrollment>{new Enrollment{Id=301,Student="Sara Ahmadi",Course="C# & .NET Fundamentals",Date=DateTime.Today.AddDays(-35),StatusKey="Active"},new Enrollment{Id=302,Student="Ali Rezaei",Course="Clean Code in Practice",Date=DateTime.Today.AddDays(-22),StatusKey="Active"},new Enrollment{Id=303,Student="Maryam Mohammadi",Course="SQL Server Essentials",Date=DateTime.Today.AddDays(-18),StatusKey="Completed"}};
 public List<AttendanceRecord> Attendance {get;}=new List<AttendanceRecord>{new AttendanceRecord{Id=401,Student="Sara Ahmadi",Course="C# & .NET Fundamentals",Date=DateTime.Today,StatusKey="Present"},new AttendanceRecord{Id=402,Student="Ali Rezaei",Course="Clean Code in Practice",Date=DateTime.Today,StatusKey="Absent"},new AttendanceRecord{Id=403,Student="Maryam Mohammadi",Course="SQL Server Essentials",Date=DateTime.Today,StatusKey="Present"}};
 public List<Assignment> Assignments {get;}=new List<Assignment>{new Assignment{Id=501,Title="SOLID Refactoring",Course="Clean Code in Practice",DueDate=DateTime.Today.AddDays(4),Submitted=21,StatusKey="Open"},new Assignment{Id=502,Title="LINQ Practice",Course="C# & .NET Fundamentals",DueDate=DateTime.Today.AddDays(7),Submitted=30,StatusKey="Open"}};
 public List<Exam> Exams {get;}=new List<Exam>{new Exam{Id=601,Title="C# Midterm",Course="C# & .NET Fundamentals",Date=DateTime.Today.AddDays(5),Questions=25,StatusKey="Scheduled"},new Exam{Id=602,Title="SQL Final",Course="SQL Server Essentials",Date=DateTime.Today.AddDays(10),Questions=35,StatusKey="Draft"}};
 public List<Grade> Grades {get;}=new List<Grade>{new Grade{Id=701,Student="Sara Ahmadi",Course="C# & .NET Fundamentals",Score=18.5m,Letter="A"},new Grade{Id=702,Student="Ali Rezaei",Course="Clean Code in Practice",Score=15.75m,Letter="B"},new Grade{Id=703,Student="Maryam Mohammadi",Course="SQL Server Essentials",Score=19.25m,Letter="A+"}};
 public List<Certificate> Certificates {get;}=new List<Certificate>{new Certificate{Id=801,Student="Maryam Mohammadi",Course="SQL Server Essentials",IssuedAt=DateTime.Today.AddDays(-2),Code="ALMS-SQL-0001"}};
 public List<TuitionInvoice> TuitionInvoices {get;}=new List<TuitionInvoice>();
 public List<TuitionPayment> TuitionPayments {get;}=new List<TuitionPayment>();
 public List<TuitionInstallment> TuitionInstallments {get;}=new List<TuitionInstallment>();
 public List<Activity> Activities {get;}=new List<Activity>{new Activity{At=DateTime.Now.AddMinutes(-18),TypeKey="EnrollmentEvent",TextKey="EnrollmentActivity"},new Activity{At=DateTime.Now.AddHours(-2),TypeKey="ExamEvent",TextKey="ExamActivity"},new Activity{At=DateTime.Now.AddHours(-5),TypeKey="CertificateEvent",TextKey="CertificateActivity"}};
 public List<AuditEntry> Audit {get;}=new List<AuditEntry>{new AuditEntry{At=DateTime.Now.AddMinutes(-12),User="admin",ActionKey="ViewedDashboard",Entity="Dashboard"},new AuditEntry{At=DateTime.Now.AddHours(-1),User="admin",ActionKey="ChangedLanguage",Entity="Settings"}};
 readonly string storePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS", "lms-data.json");
 public DataService(){ Load(); }
 void Load(){
  if(sqlStore!=null){
   try{var state=sqlStore.Load();if(state!=null)ApplyState(state);return;}
   catch(Exception ex){throw new InvalidDataException("SQL Server state could not be loaded. No fallback was attempted to prevent data divergence.",ex);}
  }
  if(!File.Exists(storePath))return;
  try {
   using(var stream=File.OpenRead(storePath)){
    var state=(LmsState)new DataContractJsonSerializer(typeof(LmsState)).ReadObject(stream);
    ApplyState(state);
   }
  }catch(Exception ex){throw new InvalidDataException("Cannot load saved LMS data. The original file has not been overwritten: " + storePath,ex);}
 }
 void ApplyState(LmsState state){
  if(state==null || state.Students==null || state.Courses==null)throw new InvalidDataException("Invalid LMS data file.");
    Students.Clear();Students.AddRange(state.Students);Courses.Clear();Courses.AddRange(state.Courses);
    if(state.Instructors!=null){Instructors.Clear();Instructors.AddRange(state.Instructors);}
    if(state.Enrollments!=null){Enrollments.Clear();Enrollments.AddRange(state.Enrollments);}
    if(state.Attendance!=null){Attendance.Clear();Attendance.AddRange(state.Attendance);}
    if(state.Assignments!=null){Assignments.Clear();Assignments.AddRange(state.Assignments);}
    if(state.Exams!=null){Exams.Clear();Exams.AddRange(state.Exams);}
    if(state.Grades!=null){Grades.Clear();Grades.AddRange(state.Grades);}
    if(state.Certificates!=null){Certificates.Clear();Certificates.AddRange(state.Certificates);}
    if(state.TuitionInvoices!=null){TuitionInvoices.Clear();TuitionInvoices.AddRange(state.TuitionInvoices);}
    if(state.TuitionPayments!=null){TuitionPayments.Clear();TuitionPayments.AddRange(state.TuitionPayments);}
    if(state.TuitionInstallments!=null){TuitionInstallments.Clear();TuitionInstallments.AddRange(state.TuitionInstallments);}
    if(state.Activities!=null){Activities.Clear();Activities.AddRange(state.Activities);}
    if(state.Audit!=null){Audit.Clear();Audit.AddRange(state.Audit);}
 }
 internal LmsState SnapshotForMigration112() { AuthorizationService.RequireAccountAdmin(); return CaptureState(); }
 LmsState CaptureState(){return new LmsState{Students=Students,Courses=Courses,Audit=Audit,Instructors=Instructors,Enrollments=Enrollments,Attendance=Attendance,Assignments=Assignments,Exams=Exams,Grades=Grades,Certificates=Certificates,Activities=Activities,TuitionInvoices=TuitionInvoices,TuitionPayments=TuitionPayments,TuitionInstallments=TuitionInstallments};}
 void Save(){
  LmsDataIntegrity.Validate(CaptureState());
  if(sqlStore!=null){sqlStore.Save(CaptureState());return;}
  var folder=Path.GetDirectoryName(storePath);Directory.CreateDirectory(folder);
  var temp=storePath+".tmp";
  try{
   using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){
    new DataContractJsonSerializer(typeof(LmsState)).WriteObject(stream,CaptureState());stream.Flush(true);
   }
   if(File.Exists(storePath))File.Replace(temp,storePath,storePath+".bak");else File.Move(temp,storePath);
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
 int nextStudent=1100,nextCourse=20;

 public Student AddStudent(string name,string email){
  AuthorizationService.RequireAcademicWrite();
 name=(name??string.Empty).Trim();email=(email??string.Empty).Trim();
 if(name.Length<2 || name.Length>120)throw new ArgumentException("Student name must contain 2 to 120 characters.","name");
 if(email.Length>254 || email.IndexOf('@')<1 || email.LastIndexOf('.')<=email.IndexOf('@')+1)throw new ArgumentException("A valid student email is required.","email");
 if(Students.Any(x=>string.Equals(x.Email,email,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("A student with this email already exists.");
 var s=new Student{Id=nextStudent=Math.Max(nextStudent,Students.Count==0?0:Students.Max(x=>x.Id))+1,Name=name,Email=email,StatusKey="Active",Progress=0};Students.Add(s);try{Log("Created","Student #"+s.Id);}catch{Students.Remove(s);throw;}return s;}
 public Course AddCourse(string title,string instructor){
  AuthorizationService.RequireAcademicWrite();
 title=(title??string.Empty).Trim();instructor=(instructor??string.Empty).Trim();
 if(title.Length<3 || title.Length>160)throw new ArgumentException("Course title must contain 3 to 160 characters.","title");
 if(instructor.Length<2 || instructor.Length>120)throw new ArgumentException("Instructor name is required.","instructor");
 if(Courses.Any(x=>string.Equals(x.Title,title,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("A course with this title already exists.");
 var c=new Course{Id=nextCourse=Math.Max(nextCourse,Courses.Count==0?0:Courses.Max(x=>x.Id))+1,Title=title,Instructor=instructor,StatusKey="Draft",Progress=0,Students=0};Courses.Add(c);try{Log("Created","Course #"+c.Id);}catch{Courses.Remove(c);throw;}return c;}

 public void UpdateStudent(int id,string name,string email){
  AuthorizationService.RequireAcademicWrite();
  var item=Students.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Student not found.");
  name=(name??"").Trim();email=(email??"").Trim();
  if(name.Length<2||name.Length>120)throw new ArgumentException("Student name must be 2-120 characters.");
  if(email.Length>254||email.IndexOf('@')<1||email.LastIndexOf('.')<=email.IndexOf('@')+1)throw new ArgumentException("Invalid email.");
  if(Students.Any(x=>x.Id!=id&&string.Equals(x.Email,email,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Email already exists.");
  if(!string.Equals(item.Name,name,StringComparison.OrdinalIgnoreCase)&& (Enrollments.Any(x=>x.Student==item.Name)||Attendance.Any(x=>x.Student==item.Name)||Grades.Any(x=>x.Student==item.Name)||Certificates.Any(x=>x.Student==item.Name)))throw new InvalidOperationException("Cannot rename a student with related records. Update relationships first.");
  var oldName=item.Name;var oldEmail=item.Email;item.Name=name;item.Email=email;
  try{Log("Updated","Student #"+id);}catch{item.Name=oldName;item.Email=oldEmail;throw;}
 }
 public void UpdateAcademicContact(int id, bool student, string phone, string address){
  AuthorizationService.RequireAcademicWrite();
  phone=(phone??"").Trim();address=(address??"").Trim();
  if(phone.Length>32 || address.Length>400 || phone.Any(c=>!char.IsDigit(c) && c!='+' && c!='-' && c!=' ' && c!='(' && c!=')'))
   throw new ArgumentException("Invalid phone or address.");
  if(student){
   var item=Students.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Student not found.");
   var oldPhone=item.Phone;var oldAddress=item.Address;item.Phone=phone;item.Address=address;
   try{Log("Updated","Student contact #"+id);}catch{item.Phone=oldPhone;item.Address=oldAddress;throw;}
  }else{
   var item=Instructors.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Instructor not found.");
   var oldPhone=item.Phone;var oldAddress=item.Address;item.Phone=phone;item.Address=address;
   try{Log("Updated","Instructor contact #"+id);}catch{item.Phone=oldPhone;item.Address=oldAddress;throw;}
  }
 }
 public void DeleteStudent(int id){
  AuthorizationService.RequireAcademicWrite();
  var item=Students.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Student not found.");
  if(Enrollments.Any(x=>x.Student==item.Name)||Attendance.Any(x=>x.Student==item.Name)||Grades.Any(x=>x.Student==item.Name)||Certificates.Any(x=>x.Student==item.Name))throw new InvalidOperationException("Student has related records and cannot be deleted.");
  int index=Students.IndexOf(item);Students.RemoveAt(index);try{Log("Deleted","Student #"+id);}catch{Students.Insert(index,item);throw;}
 }
 public void UpdateCourse(int id,string title,string instructor){
  AuthorizationService.RequireAcademicWrite();
  var item=Courses.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Course not found.");
  title=(title??"").Trim();instructor=(instructor??"").Trim();
  if(title.Length<3||title.Length>160||instructor.Length<2||instructor.Length>120)throw new ArgumentException("Invalid course title or instructor.");
  if(Courses.Any(x=>x.Id!=id&&string.Equals(x.Title,title,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Course title already exists.");
  if(!string.Equals(item.Title,title,StringComparison.OrdinalIgnoreCase)&&(Enrollments.Any(x=>x.Course==item.Title)||Attendance.Any(x=>x.Course==item.Title)||Assignments.Any(x=>x.Course==item.Title)||Exams.Any(x=>x.Course==item.Title)||Grades.Any(x=>x.Course==item.Title)||Certificates.Any(x=>x.Course==item.Title)))throw new InvalidOperationException("Cannot rename a course with related records.");
  var oldTitle=item.Title;var oldInstructor=item.Instructor;item.Title=title;item.Instructor=instructor;
  try{Log("Updated","Course #"+id);}catch{item.Title=oldTitle;item.Instructor=oldInstructor;throw;}
 }
 public void DeleteCourse(int id){
  AuthorizationService.RequireAcademicWrite();
  var item=Courses.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Course not found.");
  if(Enrollments.Any(x=>x.Course==item.Title)||Attendance.Any(x=>x.Course==item.Title)||Assignments.Any(x=>x.Course==item.Title)||Exams.Any(x=>x.Course==item.Title)||Grades.Any(x=>x.Course==item.Title)||Certificates.Any(x=>x.Course==item.Title))throw new InvalidOperationException("Course has related records and cannot be deleted.");
  int index=Courses.IndexOf(item);Courses.RemoveAt(index);try{Log("Deleted","Course #"+id);}catch{Courses.Insert(index,item);throw;}
 }

 public Instructor AddInstructor(string name,string expertise,string email){
  AuthorizationService.RequireAcademicWrite();
  name=(name??"").Trim();expertise=(expertise??"").Trim();email=(email??"").Trim();
  if(name.Length<2||name.Length>120||expertise.Length<2||expertise.Length>160)throw new ArgumentException("Instructor name or expertise is invalid.");
  if(email.Length>254||email.IndexOf('@')<1||email.LastIndexOf('.')<=email.IndexOf('@')+1)throw new ArgumentException("Invalid instructor email.");
  if(Instructors.Any(x=>string.Equals(x.Email,email,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Instructor email already exists.");
  var item=new Instructor{Id=Instructors.Count==0?201:Instructors.Max(x=>x.Id)+1,Name=name,Expertise=expertise,Email=email,Courses=0};
  Instructors.Add(item);try{Log("Created","Instructor #"+item.Id);}catch{Instructors.Remove(item);throw;}return item;
 }
 public void DeleteInstructor(int id){
  AuthorizationService.RequireAcademicWrite();
  var item=Instructors.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Instructor not found.");
  if(Courses.Any(x=>string.Equals(x.Instructor,item.Name,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Instructor is assigned to courses.");
  int index=Instructors.IndexOf(item);Instructors.RemoveAt(index);try{Log("Deleted","Instructor #"+id);}catch{Instructors.Insert(index,item);throw;}
 }
 public Enrollment EnrollStudent(int studentId,int courseId){
  AuthorizationService.RequireAcademicWrite();
  var student=Students.FirstOrDefault(x=>x.Id==studentId);var course=Courses.FirstOrDefault(x=>x.Id==courseId);
  if(student==null||course==null)throw new InvalidOperationException("Select an existing student and course.");
  if(Enrollments.Any(x=>string.Equals(x.Student,student.Name,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.Course,course.Title,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Student is already enrolled in this course.");
  var item=new Enrollment{Id=Enrollments.Count==0?301:Enrollments.Max(x=>x.Id)+1,Student=student.Name,Course=course.Title,Date=DateTime.Today,StatusKey="Active"};
  int oldCount=course.Students;Enrollments.Add(item);course.Students=Enrollments.Count(x=>x.Course==course.Title);
  try{Log("Created","Enrollment #"+item.Id);}catch{Enrollments.Remove(item);course.Students=oldCount;throw;}return item;
 }
 public void CancelEnrollment(int id){
  AuthorizationService.RequireAcademicWrite();
  var item=Enrollments.FirstOrDefault(x=>x.Id==id);if(item==null)throw new InvalidOperationException("Enrollment not found.");
  if(Attendance.Any(x=>x.Student==item.Student&&x.Course==item.Course)||Grades.Any(x=>x.Student==item.Student&&x.Course==item.Course)||Certificates.Any(x=>x.Student==item.Student&&x.Course==item.Course))throw new InvalidOperationException("Enrollment has attendance, grades or certificates.");
  var course=Courses.FirstOrDefault(x=>x.Title==item.Course);int oldCount=course==null?0:course.Students;
  int index=Enrollments.IndexOf(item);Enrollments.RemoveAt(index);if(course!=null)course.Students=Enrollments.Count(x=>x.Course==course.Title);
  try{Log("Deleted","Enrollment #"+id);}catch{Enrollments.Insert(index,item);if(course!=null)course.Students=oldCount;throw;}
 }
 public void CreateInstallmentPlan(int invoiceId, int count, DateTime firstDueDate){
  if(count<2 || count>24)throw new ArgumentOutOfRangeException("count");
  var invoice=TuitionInvoices.SingleOrDefault(x=>x.Id==invoiceId);
  if(invoice==null)throw new InvalidOperationException("Invoice not found");
  if(TuitionInstallments.Any(x=>x.InvoiceId==invoiceId))throw new InvalidOperationException("An installment plan already exists");
  var paid=TuitionPayments.Where(x=>x.InvoiceId==invoiceId).Sum(x=>x.Amount);
  if(paid>0m)throw new InvalidOperationException("Create the plan before recording payments");
  decimal baseAmount=decimal.Floor(invoice.Amount/count*100m)/100m;
  var rows=new List<TuitionInstallment>();
  for(int i=0;i<count;i++)rows.Add(new TuitionInstallment{Id=TuitionInstallments.Count==0?i+1:TuitionInstallments.Max(x=>x.Id)+i+1,InvoiceId=invoiceId,Sequence=i+1,Amount=i==count-1?invoice.Amount-baseAmount*(count-1):baseAmount,DueDate=firstDueDate.Date.AddMonths(i)});
  TuitionInstallmentService.ValidatePlan(invoice,rows);
  TuitionInstallments.AddRange(rows);
  try{Log("InstallmentPlanCreated","Invoice #"+invoiceId);}catch{foreach(var row in rows)TuitionInstallments.Remove(row);throw;}
 }
 public TuitionInvoice AddTuitionInvoice(int studentId,int courseId,decimal amount,DateTime dueDate,string currency){
  AuthorizationService.RequireAccountAdmin();
  var invoice=new TuitionInvoice{Id=TuitionInvoices.Count==0?1:TuitionInvoices.Max(x=>x.Id)+1,StudentId=studentId,CourseId=courseId,Amount=amount,DueDate=dueDate,Currency=currency};
  TuitionService.ValidateInvoice(invoice,Students,Courses);
  if(TuitionInvoices.Any(x=>x.StudentId==studentId&&x.CourseId==courseId&&x.Currency==currency))throw new InvalidOperationException("An invoice already exists for this enrollment and currency.");
  TuitionInvoices.Add(invoice);try{Log("InvoiceCreated","Invoice #"+invoice.Id);}catch{TuitionInvoices.Remove(invoice);throw;}return invoice;
 }
 public TuitionPayment AddTuitionPayment(int invoiceId,decimal amount,string reference){
  AuthorizationService.RequireAccountAdmin();
  var payment=new TuitionPayment{Id=TuitionPayments.Count==0?1:TuitionPayments.Max(x=>x.Id)+1,InvoiceId=invoiceId,Amount=amount,Reference=(reference??"").Trim(),PaidAt=DateTime.UtcNow};
  TuitionService.ValidatePayment(payment,TuitionInvoices,TuitionPayments);
  TuitionPayments.Add(payment);try{Log("PaymentRecorded","Payment #"+payment.Id);}catch{TuitionPayments.Remove(payment);throw;}return payment;
 }
 public void Log(string action,string entity){var entry=new AuditEntry{At=DateTime.Now,User=AshkanLMS.Core.AppSession.UserName,ActionKey=action,Entity=entity};Audit.Insert(0,entry);try{Save();}catch{Audit.Remove(entry);throw;}}
 public bool Login(string u,string p,out string role){return authentication.Verify(u,p,out role);}
 }}

namespace AshkanLMS.Services {
 public sealed class LmsState {
  public List<AshkanLMS.Models.Student> Students {get;set;}
  public List<AshkanLMS.Models.Course> Courses {get;set;}
  public List<AshkanLMS.Models.Instructor> Instructors {get;set;}
  public List<AshkanLMS.Models.Enrollment> Enrollments {get;set;}
  public List<AshkanLMS.Models.AttendanceRecord> Attendance {get;set;}
  public List<AshkanLMS.Models.Assignment> Assignments {get;set;}
  public List<AshkanLMS.Models.Exam> Exams {get;set;}
  public List<AshkanLMS.Models.Grade> Grades {get;set;}
  public List<AshkanLMS.Models.Certificate> Certificates {get;set;}
  public List<AshkanLMS.Models.TuitionInvoice> TuitionInvoices {get;set;}
  public List<AshkanLMS.Models.TuitionPayment> TuitionPayments {get;set;}
  public List<AshkanLMS.Models.TuitionInstallment> TuitionInstallments {get;set;}
  public List<AshkanLMS.Models.Activity> Activities {get;set;}
  public List<AshkanLMS.Models.AuditEntry> Audit {get;set;}
 }
}
