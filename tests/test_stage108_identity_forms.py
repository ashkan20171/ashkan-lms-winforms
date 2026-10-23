import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class IdentityFormTests(unittest.TestCase):
 def test_ui_ids(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage19.cs').read_text()
  for v in ('students.ValueMember="Id"','courses.ValueMember="Id"','RecordAttendance(studentId,courseId','RecordGrade(studentId,courseId','IssueCertificate(studentId,courseId'):
   self.assertIn(v,s)
 def test_service_ids(self):
  s=(ROOT/'AshkanLMS/Services/AcademicOperations.cs').read_text()
  for v in ('RecordAttendance(int studentId,int courseId','RecordGrade(int studentId,int courseId','IssueCertificate(int studentId,int courseId','StudentId=studentId','CourseId=courseId','RequireEnrollment(studentId,courseId)'):
   self.assertIn(v,s)
 def test_ambiguous_names_rejected(self):
  s=(ROOT/'AshkanLMS/Services/AcademicOperations.cs').read_text()
  self.assertEqual(s.count('Ambiguous or missing student/course'),3)
if __name__=='__main__': unittest.main()
