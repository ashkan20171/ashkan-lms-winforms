import pathlib
import unittest
import xml.etree.ElementTree as ET
ROOT = pathlib.Path(__file__).resolve().parents[1]
class AuthorizationContractTests(unittest.TestCase):
    def test_test_project_targets_framework48(self):
        path = ROOT / 'tests/Authorization.Tests/Authorization.Tests.csproj'
        root = ET.parse(path).getroot()
        ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
        self.assertEqual(root.find('.//m:TargetFrameworkVersion', ns).text, 'v4.8')
    def test_links_real_application_sources(self):
        text = (ROOT / 'tests/Authorization.Tests/Authorization.Tests.csproj').read_text()
        self.assertIn('AuthorizationService.cs', text)
        self.assertIn('AppSession.cs', text)
    def test_role_matrix_and_no_identity(self):
        text = (ROOT / 'tests/Authorization.Tests/Program.cs').read_text()
        for item in ('Administrator', 'Instructor', 'Student', 'Unknown', 'No identity', 'Whitespace identity', 'RequireAcademicWrite', 'RequireAccountAdmin'):
            self.assertIn(item, text)
    def test_windows_ci_runs_authorization_tests(self):
        self.assertIn('run-authorization-tests.ps1', (ROOT / '.github/workflows/windows-quality.yml').read_text())
