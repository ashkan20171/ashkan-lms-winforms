from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[1] / 'AshkanLMS'
class MigrationDashboardTests(unittest.TestCase):
    def test_wired_to_workspace(self):
        self.assertIn('ShowMigrationDashboard112()', (ROOT/'UI/MainForm.Stage33.cs').read_text())
    def test_compiled_in_project(self):
        self.assertIn('MainForm.Stage112.cs', (ROOT/'AshkanLMS.csproj').read_text())
    def test_confirmation_and_backup(self):
        code=(ROOT/'UI/MainForm.Stage112.cs').read_text()
        for token in ['backup.Checked', 'typed.Text == "IMPORT"', 'PreviewCore(', 'ImportCore(', 'RequireAccountAdmin()', 'importButton.Enabled = false']:
            self.assertIn(token, code)
    def test_snapshot_is_admin_restricted(self):
        self.assertIn('SnapshotForMigration112() { AuthorizationService.RequireAccountAdmin();', (ROOT/'Services/DataService.cs').read_text())
