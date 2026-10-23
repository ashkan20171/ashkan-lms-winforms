import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Stage118Audit(unittest.TestCase):
    def test_service_registered(self):
        self.assertIn('Services\\DeploymentAuditTrail.cs', (ROOT/'AshkanLMS/AshkanLMS.csproj').read_text(encoding='utf-8-sig'))
    def test_admin_gate_and_no_exception_logging(self):
        text = (ROOT/'AshkanLMS/Services/DeploymentAuditTrail.cs').read_text()
        self.assertIn('AuthorizationService.RequireAccountAdmin()', text)
        self.assertIn('LocalApplicationData', text)
        self.assertIn('FileMode.Append', text)
    def test_ui_integration(self):
        text = (ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text(encoding='utf-8-sig')
        for action in ('SQL_PREFLIGHT', 'MIGRATION_PREVIEW', 'MIGRATION_IMPORT', 'MIGRATION_VERIFY'):
            self.assertIn(action, text)
        self.assertIn('DeploymentAuditTrail.ToCsv', text)
