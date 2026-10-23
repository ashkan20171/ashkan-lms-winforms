import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Finance84(unittest.TestCase):
 def test_navigation(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text();self.assertIn('"Reports","Tuition","SecurityCenter"',s)
 def test_localization(self):
  self.assertIn('"Tuition",new[]', (ROOT/'AshkanLMS/Core/Localization.cs').read_text())
 def test_currency_separation(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage28.cs').read_text();self.assertIn('new [] { "IRR", "EUR" }',s);self.assertIn('TuitionService.Summarize',s)
 def test_csv_export(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage83.cs').read_text();self.assertIn('CsvSafe',s);self.assertIn('SaveFileDialog',s)
 def test_filter(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage83.cs').read_text();self.assertIn('search.TextChanged',s);self.assertIn('from.ValueChanged',s)
if __name__=='__main__':unittest.main()
