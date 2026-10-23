import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1] / 'AshkanLMS'
class Stage115Tests(unittest.TestCase):
    def test_details_and_csv(self):
        source = (ROOT / 'Services' / 'RelationalMigrationService.cs').read_text(encoding='utf-8')
        for token in ('class VerificationDetail', 'AddDetails(report, name', 'ExpectedValue,ActualValue', 'report.Details.Add'):
            self.assertIn(token, source)
    def test_dialog_and_filter(self):
        source = (ROOT / 'UI' / 'MainForm.Stage112.cs').read_text(encoding='utf-8')
        for token in ('detailDialog', 'SelectedIndexChanged', 'result.Details.Where', 'detailsGrid.Rows.Add'):
            self.assertIn(token, source)
