using System;
using System.Linq;
using AshkanLMS.Models;
namespace AshkanLMS.Services {
 public sealed class AcademicOperations {
  readonly DataService db;
  public AcademicOperations(DataService db){this.db=db??throw new ArgumentNullException("db");}
  void RequireCourse(string course){if(!db.Courses.Any(x=>x.Title==course))throw new InvalidOperationException("Course not found.");}
  void RequireEnrollment(string student,string course){if(!db.Enrollments.Any(x=>x.Student==student&&x.Course==course&&x.StatusKey!="Cancelled"))throw new InvalidOperationException("An active enrollment is required.");}
  public Exam ScheduleExam(string title,string course,DateTime date,int questions){
   AuthorizationService.RequireAcademicWrite();
   if(string.IsNullOrWhiteSpace(title)||title.Trim().Length>160)throw new ArgumentException("Invalid exam title.");RequireCourse(course);
   if(questions<1||questions>500)throw new ArgumentOutOfRangeException("questions");
   if(db.Exams.Any(x=>x.Course==course&&x.Title.Equals(title.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Duplicate exam title for course.");
   var item=new Exam{Id=db.Exams.Count==0?1:db.Exams.Max(x=>x.Id)+1,Title=title.Trim(),Course=course,Date=date,Questions=questions,StatusKey="Scheduled"};
   db.Exams.Add(item);try{db.Log("ScheduledExam","Exam #"+item.Id);}catch{db.Exams.Remove(item);throw;}return item;
  }
  public Assignment CreateAssignment(string title,string course,DateTime due){
   AuthorizationService.RequireAcademicWrite();
   if(string.IsNullOrWhiteSpace(title)||title.Trim().Length>160)throw new ArgumentException("Invalid assignment title.");RequireCourse(course);
   var item=new Assignment{Id=db.Assignments.Count==0?1:db.Assignments.Max(x=>x.Id)+1,Title=title.Trim(),Course=course,DueDate=due,StatusKey="Open",Submitted=0};
   db.Assignments.Add(item);try{db.Log("CreatedAssignment","Assignment #"+item.Id);}catch{db.Assignments.Remove(item);throw;}return item;
  }
  public AttendanceRecord RecordAttendance(string student,string course,DateTime day,bool present){
   AuthorizationService.RequireAcademicWrite();
   RequireEnrollment(student,course);day=day.Date;
   if(db.Attendance.Any(x=>x.Student==student&&x.Course==course&&x.Date.Date==day))throw new InvalidOperationException("Attendance already recorded for this date.");
   var item=new AttendanceRecord{Id=db.Attendance.Count==0?1:db.Attendance.Max(x=>x.Id)+1,Student=student,Course=course,Date=day,StatusKey=present?"Present":"Absent"};
   db.Attendance.Add(item);try{db.Log("RecordedAttendance","Attendance #"+item.Id);}catch{db.Attendance.Remove(item);throw;}return item;
  }
  public Grade RecordGrade(string student,string course,decimal score){
   AuthorizationService.RequireAcademicWrite();
   RequireEnrollment(student,course);if(score<0||score>20)throw new ArgumentOutOfRangeException("score","Score must be between 0 and 20.");
   if(db.Grades.Any(x=>x.Student==student&&x.Course==course))throw new InvalidOperationException("A grade already exists for this enrollment.");
   var item=new Grade{Id=db.Grades.Count==0?1:db.Grades.Max(x=>x.Id)+1,Student=student,Course=course,Score=score,Letter=score>=18?"A":score>=16?"B":score>=14?"C":score>=10?"D":"F"};
   db.Grades.Add(item);try{db.Log("RecordedGrade","Grade #"+item.Id);}catch{db.Grades.Remove(item);throw;}return item;
  }
  public Certificate IssueCertificate(string student,string course){
   AuthorizationService.RequireAcademicWrite();
   RequireEnrollment(student,course);
   if(db.Certificates.Any(x=>x.Student==student&&x.Course==course))throw new InvalidOperationException("Certificate already issued.");
   if(!db.Grades.Any(x=>x.Student==student&&x.Course==course&&x.Score>=10))throw new InvalidOperationException("A passing grade is required.");
   var item=new Certificate{Id=db.Certificates.Count==0?1:db.Certificates.Max(x=>x.Id)+1,Student=student,Course=course,IssuedAt=DateTime.Now,Code="ALMS-"+Guid.NewGuid().ToString("N").Substring(0,12).ToUpperInvariant()};
   db.Certificates.Add(item);try{db.Log("IssuedCertificate",item.Code);}catch{db.Certificates.Remove(item);throw;}return item;
  }
 }
}
