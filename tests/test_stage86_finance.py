import pathlib,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class Stage86(unittest.TestCase):
 def test_chart(self):
  s=(ROOT/'AshkanLMS/UI/RevenueChart.cs').read_text()
  for term in ('OnPaint','DoubleBuffered','SetData','MonthlyRevenue','FillRectangle'): self.assertIn(term,s)
 def test_wired(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage85.cs').read_text();self.assertIn('new RevenueChart',s);self.assertIn('trend.SetData(monthly)',s)
 def test_bilingual(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage85.cs').read_text();self.assertIn('DirectionRightToLeft',s);self.assertIn('صورتحساب',s);self.assertIn('PrintPreviewDialog',s)
 def test_project(self): self.assertIn('UI\\RevenueChart.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
 def test_notes(self): self.assertIn('Microsoft Print to PDF',(ROOT/'STAGE86-NOTES.md').read_text())
