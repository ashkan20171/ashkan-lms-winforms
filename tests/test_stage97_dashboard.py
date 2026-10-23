import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Dashboard97Tests(unittest.TestCase):
 def test_dashboard_calls_widgets(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage28.cs').read_text()
  self.assertIn('AddDashboardPriorityAlerts97(host,fa)',s)
  self.assertIn('AddDashboardAcademicPulse97(host,fa)',s)
 def test_responsive_columns(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage28.cs').read_text()
  self.assertIn('available>=1050?4',s)
  self.assertIn('available>=700?3',s)
 def test_admin_guard(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage97.cs').read_text()
  self.assertIn('AppSession.Role=="Administrator" && settings.ShowFinancial',s)
 def test_actual_data(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage97.cs').read_text()
  self.assertIn('data.TuitionPayments',s)
  self.assertIn('data.Exams.Count',s)
 def test_registered(self):
  self.assertIn('MainForm.Stage97.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
if __name__=='__main__':unittest.main()
