import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Academic102(unittest.TestCase):
    def test_helper_registered(self):
        self.assertIn('MainForm.Stage102.cs', (ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
    def test_resize_lifecycle(self):
        src=(ROOT/'AshkanLMS/UI/MainForm.Stage102.cs').read_text()
        self.assertIn('content.Resize += resize', src)
        self.assertIn('content.Resize -= resize', src)
    def test_email_validation(self):
        self.assertIn('new MailAddress', (ROOT/'AshkanLMS/UI/MainForm.Stage102.cs').read_text())
    def test_student_and_course_pages(self):
        self.assertGreaterEqual((ROOT/'AshkanLMS/UI/MainForm.cs').read_text().count('FitAcademicCard102(c,g)'),2)
    def test_instructor_enrollment_pages(self):
        self.assertEqual((ROOT/'AshkanLMS/UI/MainForm.Stage17.cs').read_text().count('FitAcademicCard102(card,grid)'),2)
    def test_bilingual_confirm(self):
        src=(ROOT/'AshkanLMS/UI/MainForm.Stage17.cs').read_text()
        self.assertIn('مدرس انتخاب‌شده',src)
        self.assertIn('ثبت‌نام انتخاب‌شده',src)
if __name__=='__main__':unittest.main()
