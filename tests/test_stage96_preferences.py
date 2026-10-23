import unittest
from pathlib import Path
R=Path(__file__).resolve().parents[1]/'AshkanLMS'
class Stage96(unittest.TestCase):
 def test_preferences_source(self): self.assertTrue((R/'Services/NotificationPreferencesService.cs').exists())
 def test_preferences_ui(self): self.assertTrue((R/'UI/MainForm.Stage96.cs').exists())
 def test_navigation(self): self.assertIn('ShowNotificationPreferences96()', (R/'UI/MainForm.cs').read_text())
 def test_dashboard(self): self.assertIn('AddDashboardNotificationSummary96()', (R/'UI/MainForm.cs').read_text())
 def test_csproj(self): self.assertIn('MainForm.Stage96.cs',(R/'AshkanLMS.csproj').read_text())
 def test_validation(self): self.assertIn('Math.Min(90', (R/'Services/NotificationPreferencesService.cs').read_text())
