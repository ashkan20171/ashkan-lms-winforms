import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]/"AshkanLMS"
class Stage91(unittest.TestCase):
 def test_no_duplicate_model(self):
  self.assertEqual((ROOT/'Models/Entities.cs').read_text().count('public class TuitionInstallment {'),1)
 def test_project_compiles_new_files(self):
  s=(ROOT/'AshkanLMS.csproj').read_text()
  for name in ('InstallmentReportingService.cs','MainForm.Stage91.cs'):self.assertIn(name,s)
 def test_navigation(self):
  s=(ROOT/'UI/MainForm.cs').read_text()
  self.assertIn('ShowInstallmentReports()',s);self.assertIn('key=="InstallmentReports"',s)
 def test_localization(self):
  self.assertIn('گزارش اقساط',(ROOT/'Core/Localization.cs').read_text())
 def test_reporting(self):
  s=(ROOT/'Services/InstallmentReportingService.cs').read_text()
  for name in ('Calculate(group,pay,asOf)','x.Currency==currency','Csv(','DueDate.Date'):self.assertIn(name,s)
 def test_csv_and_admin(self):
  s=(ROOT/'UI/MainForm.Stage91.cs').read_text()
  self.assertIn('AppSession.Role!="Administrator"',s);self.assertIn('SaveFileDialog',s)
if __name__=='__main__':unittest.main()
