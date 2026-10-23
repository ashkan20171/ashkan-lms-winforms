import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Stage122Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.queue=(ROOT/'AshkanLMS/Services/DeploymentAuditOutbox.cs').read_text()
        cls.ui=(ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text(encoding='utf-8-sig')
    def test_quarantine_moves_not_deletes(self):
        self.assertIn('File.Move(file, destination)',self.queue)
        self.assertIn('"*.damaged"',self.queue)
    def test_corruption_does_not_block_other_events(self):
        self.assertIn('Quarantine(file);',self.queue)
        self.assertIn('continue; // Other valid entries',self.queue)
    def test_cooldown(self):
        self.assertIn('NextRetryUtc',self.queue)
        self.assertIn('TimeSpan.FromSeconds(60)',self.queue)
    def test_ui_reports_quarantine(self):
        self.assertIn('InspectQuarantine(500)',self.ui)
        self.assertIn('QuarantinedCount',self.ui)
    def test_admin_access(self):
        self.assertIn('internal static string[] InspectQuarantine(int limit)',self.queue)
        self.assertIn('AuthorizationService.RequireAccountAdmin();',self.queue)
