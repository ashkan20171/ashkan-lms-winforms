import pathlib
import unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class AuthenticationContract(unittest.TestCase):
 def test_isolated_constructor(self):
  self.assertIn('AuthenticationService(string dataFolder)',(ROOT/'AshkanLMS/Services/AuthenticationService.cs').read_text())
 def test_audit_isolation(self):
  self.assertIn('SecurityAuditService(string dataFolder)',(ROOT/'AshkanLMS/Services/SecurityAuditService.cs').read_text())
 def test_test_project(self):
  self.assertTrue((ROOT/'tests/Authentication.Tests/Authentication.Tests.csproj').exists())
 def test_windows_ci(self):
  self.assertIn('run-authentication-tests.ps1',(ROOT/'.github/workflows/windows-quality.yml').read_text())
if __name__=='__main__':unittest.main()
