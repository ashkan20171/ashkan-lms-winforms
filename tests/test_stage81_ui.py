import pathlib,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class Stage81(unittest.TestCase):
 def test_chart_compiled(self):
  self.assertIn('UI\\SecurityTrendChart.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
 def test_chart_data(self):
  s=(ROOT/'AshkanLMS/UI/SecurityTrendChart.cs').read_text();self.assertIn('SetEvents',s);self.assertIn('DoubleBuffered=true',s)
 def test_kpis_and_resize(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage26.cs').read_text();self.assertIn('metricLabels',s);self.assertIn('AnchorStyles',s);self.assertIn('trend.SetEvents(snapshot)',s)
