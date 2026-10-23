import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class Stage89Tests(unittest.TestCase):
 def test_status_helper(self):
  s=(ROOT/'AshkanLMS/UI/TuitionVisualStatus.cs').read_text()
  for key in ['Paid','Partial','Unpaid','Overdue','StyleGrid','CellFormatting']: self.assertIn(key,s)
 def test_filter(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage83.cs').read_text()
  self.assertIn('overdueOnly.Checked',s)
  self.assertIn('overdueOnly.CheckedChanged',s)
 def test_export(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage83.cs').read_text()
  self.assertIn('Balance,Status',s)
  self.assertIn('Enumerable.Range(0,9)',s)
 def test_project(self):
  self.assertIn('UI\\TuitionVisualStatus.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
if __name__=='__main__':unittest.main()
