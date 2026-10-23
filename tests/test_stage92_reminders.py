import pathlib,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class ReminderStage92Tests(unittest.TestCase):
 def test_service(self):
  s=(ROOT/'AshkanLMS/Services/InstallmentReminderService.cs').read_text()
  for v in ['GetPending','Outstanding>0m','Due today','Overdue','Draft(','Csv(','ReminderAcknowledgementStore','LocalApplicationData']:
   with self.subTest(v=v):self.assertIn(v,s)
 def test_ui_and_wiring(self):
  m=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8-sig')
  for v in ['"PaymentReminders"','ShowPaymentReminders()','key=="PaymentReminders"']:self.assertIn(v,m)
  u=(ROOT/'AshkanLMS/UI/MainForm.Stage92.cs').read_text()
  for v in ['AppSession.Role!="Administrator"','Clipboard.SetText','ReminderAcknowledgementStore.Mark','SaveFileDialog','NumericUpDown']:self.assertIn(v,u)
 def test_build_includes(self):
  s=(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text()
  self.assertIn('MainForm.Stage92.cs',s);self.assertIn('InstallmentReminderService.cs',s)
if __name__=='__main__':unittest.main()
