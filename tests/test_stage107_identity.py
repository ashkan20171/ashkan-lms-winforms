import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'AshkanLMS'
class IdentityStage107(unittest.TestCase):
    def test_entities_have_ids(self):
        s=(ROOT/'Models/Entities.cs').read_text()
        for cls in ('AttendanceRecord','Grade','Certificate'):
            self.assertIn('public class '+cls+' { public int Id{get;set;} public int StudentId{get;set;} public int CourseId{get;set;}',s)
    def test_linking(self):
        s=(ROOT/'Services/AcademicIdentityService.cs').read_text()
        for name in ('Attendance','Grades','Certificates'):
            self.assertIn('data.'+name,s)
    def test_no_duplicate_property(self):
        s=(ROOT/'Services/DataService.cs').read_text()
        self.assertEqual(s.count('public List<TuitionInstallment> TuitionInstallments'),1)
    def test_reference_validation(self):
        s=(ROOT/'Services/LmsDataIntegrity.cs').read_text()
        for name in ('Attendance','Grades','Certificates'):
            self.assertIn('state.'+name,s)
if __name__=='__main__':unittest.main()
