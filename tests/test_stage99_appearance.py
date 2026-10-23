import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]/'AshkanLMS'
class AppearanceTests(unittest.TestCase):
 def test_theme(self):
  s=(ROOT/'Core'/'Theme.cs').read_text();self.assertIn('SetDarkMode(bool enabled)',s);self.assertIn('DarkMode',s)
 def test_persistence(self):
  s=(ROOT/'Core'/'AppearancePreferencesService.cs').read_text();self.assertIn('LocalApplicationData',s);self.assertIn('SaveDarkMode',s)
 def test_navigation(self):
  s=(ROOT/'UI'/'MainForm.cs').read_text();self.assertIn('ShowAppearanceSettings99()',s);self.assertIn('AppearancePreferencesService.LoadDarkMode()',s)
 def test_project(self):
  s=(ROOT/'AshkanLMS.csproj').read_text();self.assertIn('MainForm.Stage99.cs',s);self.assertIn('AppearancePreferencesService.cs',s)
if __name__=='__main__':unittest.main()
