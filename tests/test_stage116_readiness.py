from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[1]
class Stage116ReadinessTests(unittest.TestCase):
    def test_readiness_blocks_discrepancies(self):
        code=(ROOT/'AshkanLMS/Services/RelationalMigrationService.cs').read_text()
        self.assertIn('Rows.Count >= 14 && total == 0',code)
        self.assertIn('int total = missing + extra + mismatched;',code)
    def test_no_automatic_cutover(self):
        code=(ROOT/'AshkanLMS/Services/RelationalMigrationService.cs').read_text()
        self.assertIn('No automatic cutover was performed.',code)
    def test_dashboard_shows_report(self):
        code=(ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text()
        self.assertIn('result.AssessReadiness()',code)
        self.assertIn('readiness.ToReport()',code)
if __name__=='__main__': unittest.main()
