import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Stage117Tests(unittest.TestCase):
    def test_preflight_is_in_project(self):
        self.assertIn('Services\\SqlDeploymentPreflight.cs', next((ROOT/'AshkanLMS').glob('*.csproj')).read_text(encoding='utf-8-sig'))
    def test_preflight_read_only(self):
        text = (ROOT/'AshkanLMS/Services/SqlDeploymentPreflight.cs').read_text()
        self.assertIn('OBJECT_ID(@name', text)
        self.assertNotIn('INSERT INTO', text)
        self.assertNotIn('DELETE FROM', text)
    def test_admin_guard(self):
        text = (ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text()
        self.assertIn('SqlDeploymentPreflight.Run', text)
        self.assertIn('AuthorizationService.RequireAccountAdmin();', text)
    def test_restore_not_claimed(self):
        text = (ROOT/'AshkanLMS/Services/SqlDeploymentPreflight.cs').read_text()
        self.assertIn('restore not tested', text)
