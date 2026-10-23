using System;
namespace AshkanLMS.Models {
 public class Student { public int Id{get;set;} public string Name{get;set;} public string Email{get;set;} public string StatusKey{get;set;} public int Progress{get;set;} public string Phone{get;set;} public string Address{get;set;} }
 public class Course { public int Id{get;set;} public string Title{get;set;} public string Instructor{get;set;} public int InstructorId{get;set;} public int Students{get;set;} public int Progress{get;set;} public string StatusKey{get;set;} }
 public class Instructor { public int Id{get;set;} public string Name{get;set;} public string Expertise{get;set;} public string Email{get;set;} public int Courses{get;set;} public string Phone{get;set;} public string Address{get;set;} }
 public class Enrollment { public int Id{get;set;} public string Student{get;set;} public int StudentId{get;set;} public string Course{get;set;} public int CourseId{get;set;} public DateTime Date{get;set;} public string StatusKey{get;set;} }
 public class AttendanceRecord { public int Id{get;set;} public int StudentId{get;set;} public int CourseId{get;set;} public string Student{get;set;} public string Course{get;set;} public DateTime Date{get;set;} public string StatusKey{get;set;} }
 public class Assignment { public int Id{get;set;} public string Title{get;set;} public string Course{get;set;} public DateTime DueDate{get;set;} public int Submitted{get;set;} public string StatusKey{get;set;} }
 public class Exam { public int Id{get;set;} public string Title{get;set;} public string Course{get;set;} public DateTime Date{get;set;} public int Questions{get;set;} public string StatusKey{get;set;} }
 public class Grade { public int Id{get;set;} public int StudentId{get;set;} public int CourseId{get;set;} public string Student{get;set;} public string Course{get;set;} public decimal Score{get;set;} public string Letter{get;set;} }
 public class Certificate { public int Id{get;set;} public int StudentId{get;set;} public int CourseId{get;set;} public string Student{get;set;} public string Course{get;set;} public DateTime IssuedAt{get;set;} public string Code{get;set;} }
 public class TuitionInvoice { public int Id{get;set;} public int StudentId{get;set;} public int CourseId{get;set;} public decimal Amount{get;set;} public DateTime DueDate{get;set;} public string Currency{get;set;} }
 public class TuitionInstallment { public int Id{get;set;} public int InvoiceId{get;set;} public int Sequence{get;set;} public decimal Amount{get;set;} public DateTime DueDate{get;set;} }
 public class TuitionPayment { public int Id{get;set;} public int InvoiceId{get;set;} public decimal Amount{get;set;} public DateTime PaidAt{get;set;} public string Reference{get;set;} }
 public class Activity { public DateTime At{get;set;} public string TypeKey{get;set;} public string TextKey{get;set;} }
 public class AuditEntry { public DateTime At{get;set;} public string User{get;set;} public string ActionKey{get;set;} public string Entity{get;set;} }
}
