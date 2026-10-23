import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Stage98NavigationTests(unittest.TestCase):
 def test_navigation_source_in_project(self):
  self.assertIn('UI\\MainForm.Stage98.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text(encoding='utf-8-sig'))
 def test_search_filters_authorized_pages(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage98.cs').read_text(encoding='utf-8-sig')
  self.assertIn('IsPageAuthorized(k)',s)
  self.assertIn('navigationSearch98.TextChanged',s)
 def test_bilingual_groups(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage98.cs').read_text(encoding='utf-8-sig')
  self.assertIn('خانه و اعلان‌ها',s)
  self.assertIn('Administration',s)
 def test_active_selection(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8-sig')
  self.assertIn('RefreshNavigationSelection98();',s)
 def test_no_duplicate_resize_handlers(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8-sig')
  self.assertIn('Resize-=ShellResized98;Resize+=ShellResized98;',s)
