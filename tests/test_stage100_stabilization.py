import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAIN = (ROOT / 'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8')

class Stage100StabilizationTests(unittest.TestCase):
    def test_student_empty_state(self):
        self.assertIn('if(st==null)st=data.Students.FirstOrDefault();if(st==null)', MAIN)
    def test_course_empty_state(self):
        self.assertIn('if(co==null){info.Text=', MAIN)
    def test_course_null_selection(self):
        self.assertIn('combo.SelectedItem==null?null', MAIN)
    def test_financial_role_restriction(self):
        self.assertIn('key=="FinanceReports"||key=="Tuition"', MAIN)
    def test_csv_formula_guard(self):
        self.assertIn('"=+-@".IndexOf(trimmed[0])>=0', MAIN)
    def test_csv_quote_escaping(self):
        self.assertIn('value.Replace("\\\"","\\\"\\\"")', MAIN)
    def test_csv_applied_to_student_export(self):
        self.assertIn('CsvCell100(x.Email)', MAIN)
    def test_project_includes_main(self):
        self.assertIn('UI\\MainForm.cs', (ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())

if __name__ == '__main__': unittest.main()
