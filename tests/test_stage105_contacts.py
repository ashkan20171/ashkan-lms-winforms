import pathlib,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]/'AshkanLMS'
class Contacts(unittest.TestCase):
 def test_models(self):
  s=(ROOT/'Models/Entities.cs').read_text();self.assertGreaterEqual(s.count('public string Phone{get;set;}'),2)
 def test_authorization(self):
  s=(ROOT/'Services/DataService.cs').read_text();self.assertIn('UpdateAcademicContact',s);self.assertIn('AuthorizationService.RequireAcademicWrite();',s)
 def test_editor(self):
  s=(ROOT/'UI/MainForm.Stage104.cs').read_text();self.assertIn('data.UpdateAcademicContact',s)
 def test_duplicate_initializer_fixed(self):
  s=(ROOT/'Services/DataService.cs').read_text();self.assertNotIn('TuitionInstallments=TuitionInstallments,TuitionInstallments',s)
