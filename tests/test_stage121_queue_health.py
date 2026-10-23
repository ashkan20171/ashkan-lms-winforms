import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class QueueHealthTests(unittest.TestCase):
    def test_admin_only_inspection(self):
        source=(ROOT/'AshkanLMS/Services/DeploymentAuditOutbox.cs').read_text()
        self.assertIn('internal static string[] InspectPending(int limit)',source)
        self.assertIn('AuthorizationService.RequireAccountAdmin();',source)
    def test_corruption_detected(self):
        source=(ROOT/'AshkanLMS/Services/DeploymentAuditOutbox.cs').read_text()
        self.assertIn('"DAMAGED"',source)
        self.assertIn('Path.GetFileNameWithoutExtension(file)',source)
    def test_inspection_ui(self):
        source=(ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text(encoding='utf-8-sig')
        self.assertIn('DeploymentAuditOutbox.InspectPending(500)',source)
        self.assertIn('viewer.ShowDialog(dialog)',source)
