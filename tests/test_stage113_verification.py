from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[1] / 'AshkanLMS'
class Stage113(unittest.TestCase):
    def test_verify_read_only(self):
        s=(ROOT/'Services/RelationalMigrationService.cs').read_text()
        self.assertIn('internal MigrationVerification VerifyTarget(LmsState state)',s)
        self.assertIn('SELECT AttendanceId,EnrollmentId',s)
        self.assertIn('expected.Except(actual).Count()',s)
    def test_csv(self):
        s=(ROOT/'Services/RelationalMigrationService.cs').read_text()
        self.assertIn('Check,Expected,Actual,Missing,Extra,Mismatched',s)
    def test_ui(self):
        s=(ROOT/'UI/MainForm.Stage112.cs').read_text()
        self.assertIn('verifyButton.Click +=',s)
        self.assertIn('new UTF8Encoding(true)',s)
