import unittest
from pathlib import Path
R=Path(__file__).resolve().parents[1]/"AshkanLMS"
class Stage95Tests(unittest.TestCase):
 def test_navigation(self):
  t=(R/"UI/MainForm.cs").read_text(encoding="utf-8-sig");self.assertIn('ShowNotifications(){ShowNotificationCenter95();}',t)
 def test_project(self):
  t=(R/"AshkanLMS.csproj").read_text(encoding="utf-8-sig");self.assertIn('NotificationCenterService.cs',t);self.assertIn('MainForm.Stage95.cs',t)
 def test_service(self):
  t=(R/"Services/NotificationCenterService.cs").read_text();self.assertIn('GetPending',t);self.assertIn('UnreadCount',t);self.assertIn('NotificationReadStore',t)
 def test_ui(self):
  t=(R/"UI/MainForm.Stage95.cs").read_text();self.assertIn('Mark all read',t);self.assertIn('Administrator',t);self.assertIn('Unread only',t)
if __name__=='__main__':unittest.main()
