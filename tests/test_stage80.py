import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Stage80Tests(unittest.TestCase):
 def test_service_in_project(self):
  self.assertIn('SecurityDashboardService.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
 def test_aggregations(self):
  s=(ROOT/'AshkanLMS/Services/SecurityDashboardService.cs').read_text()
  for token in ['LoginFailed','AccountLocked','SessionIdleTimeout','SessionRevoked','ExportCsv']:
   self.assertIn(token,s)
 def test_ui_wiring(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage26.cs').read_text()
  self.assertIn('SecurityDashboardService.Aggregate(snapshot)',s)
  self.assertIn('SecurityDashboardService.ExportCsv(snapshot)',s)
  self.assertIn('RequireAccountAdmin()',s)
if __name__=='__main__':unittest.main()
