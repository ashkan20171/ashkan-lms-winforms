import pathlib, unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Stage73Contract(unittest.TestCase):
    def test_pure_service_registered(self):
        self.assertIn('Services\\DiagnosticSummaryService.cs', (ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
    def test_summary_is_ui_independent(self):
        code=(ROOT/'AshkanLMS/Services/DiagnosticSummaryService.cs').read_text()
        self.assertNotIn('System.Windows.Forms',code)
        self.assertIn('Aggregate(IEnumerable<Event>',code)
    def test_csv_safe_contract(self):
        code=(ROOT/'AshkanLMS/Services/DiagnosticSummaryService.cs').read_text()
        self.assertIn('SafeType(item.Type)',code)
        self.assertIn('Category,ErrorType,Count,FirstUtc,LastUtc',code)
    def test_ui_uses_service(self):
        code=(ROOT/'AshkanLMS/UI/MainForm.Stage69.cs').read_text()
        self.assertIn('DiagnosticSummaryService.Aggregate',code)
        self.assertIn('DiagnosticSummaryService.ExportCsv',code)
if __name__=='__main__': unittest.main()
