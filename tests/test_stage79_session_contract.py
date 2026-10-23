import pathlib,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class SessionContract(unittest.TestCase):
 def test_service_compiled(self):
  self.assertIn('Services\\SessionLifetimeService.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text(encoding='utf-8-sig'))
 def test_idle_check_in_main(self):
  source=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8-sig')
  self.assertIn('idlePolicy.IsExpired',source)
  self.assertIn('Application.RemoveMessageFilter',source)
 def test_unit_project_uses_production_source(self):
  self.assertIn('SessionLifetimeService.cs',(ROOT/'tests/SessionLifetime.Tests/SessionLifetime.Tests.csproj').read_text())
 def test_ci_runs_tests(self):
  self.assertIn('run-session-tests.ps1',(ROOT/'.github/workflows/windows-quality.yml').read_text())
