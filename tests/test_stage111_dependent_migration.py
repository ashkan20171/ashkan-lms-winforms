import pathlib, unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = (ROOT / 'AshkanLMS/Services/RelationalMigrationService.cs').read_text(encoding='utf-8')
class Stage111Tests(unittest.TestCase):
    def test_attendance_import(self): self.assertIn('INSERT dbo.LmsAttendance', SOURCE)
    def test_grade_import(self): self.assertIn('INSERT dbo.LmsGrades', SOURCE)
    def test_certificate_import(self): self.assertIn('INSERT dbo.LmsCertificates', SOURCE)
    def test_rollback(self): self.assertIn('transaction.Rollback()', SOURCE)
    def test_ambiguous_name_rejected(self): self.assertIn('Ambiguous ', SOURCE)
    def test_enrollment_guard(self): self.assertIn('Missing enrollment for ', SOURCE)
