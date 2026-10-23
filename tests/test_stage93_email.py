import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]/'AshkanLMS'
class Stage93(unittest.TestCase):
 def test_service_in_project(self):
  p=(ROOT/'AshkanLMS.csproj').read_text();self.assertIn('ReminderEmailService.cs',p);self.assertIn('MainForm.Stage93.cs',p)
 def test_no_automatic_send(self):
  s=(ROOT/'UI/MainForm.Stage93.cs').read_text();self.assertIn('MessageBoxButtons.YesNo',s);self.assertIn('ReminderEmailService.Send(request)',s)
 def test_tls(self):
  s=(ROOT/'Services/ReminderEmailService.cs').read_text();self.assertIn('smtp.EnableSsl=true',s);self.assertIn('if(!r.EnableSsl)',s)
 def test_history_only_after_send(self):
  s=(ROOT/'UI/MainForm.Stage93.cs').read_text();self.assertLess(s.index('ReminderEmailService.Send(request)'),s.index('ReminderEmailService.RecordAccepted'))
 def test_no_password_persistence(self):
  s=(ROOT/'Services/ReminderEmailService.cs').read_text();self.assertNotIn('r.Password+Environment.NewLine',s)
if __name__=='__main__':unittest.main()
