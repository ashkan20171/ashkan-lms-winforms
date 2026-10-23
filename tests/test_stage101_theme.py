import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MAIN=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8')
STAGE=(ROOT/'AshkanLMS/UI/MainForm.Stage101.cs').read_text(encoding='utf-8')
CSPROJ=(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text(encoding='utf-8')
class Stage101ThemeTests(unittest.TestCase):
 def test_hook(self): self.assertIn('LayoutDirectionManager.ApplyTo(content);ApplyPageTheme101(content);',MAIN)
 def test_project(self): self.assertIn('UI\\MainForm.Stage101.cs',CSPROJ)
 def test_grid(self): self.assertIn('EnableHeadersVisualStyles = false',STAGE)
 def test_dark(self): self.assertIn('Theme.DarkMode ?',STAGE)
 def test_rtl(self): self.assertIn('AppSession.Persian ? RightToLeft.Yes',STAGE)
 def test_input(self): self.assertIn('control is TextBox || control is ComboBox',STAGE)
 def test_recursive(self): self.assertIn('ApplyPageTheme101(control);',STAGE)
 def test_selection(self): self.assertIn('SelectionForeColor = Theme.Text',STAGE)
if __name__=='__main__': unittest.main()
