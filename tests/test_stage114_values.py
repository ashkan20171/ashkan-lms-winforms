import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / 'AshkanLMS/Services/RelationalMigrationService.cs').read_text(encoding='utf-8')
class ValueVerificationTests(unittest.TestCase):
    def test_all_seven_entities_checked(self):
        for name in ('Student details', 'Instructor details', 'Course details', 'Enrollment details', 'Attendance details', 'Grade details', 'Certificate details'):
            self.assertIn('"' + name + '"', SERVICE)
    def test_value_comparison_is_read_only(self):
        self.assertIn('private static void VerifyValues', SERVICE)
        self.assertIn('SequenceEqual(actual[pair.Key]', SERVICE)
    def test_datetime_and_decimal_normalization(self):
        self.assertIn('yyyy-MM-ddTHH:mm:ss.fffffff', SERVICE)
        self.assertIn('G29', SERVICE)
if __name__ == '__main__': unittest.main()
