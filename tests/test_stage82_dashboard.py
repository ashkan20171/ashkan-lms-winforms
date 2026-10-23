import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = (ROOT / "AshkanLMS/UI/MainForm.Stage28.cs").read_text(encoding="utf-8")

class Stage82DashboardTests(unittest.TestCase):
    def test_real_enrollment_dates(self):
        self.assertIn("e.Date>=m&&e.Date<m.AddMonths(1)", SOURCE)
    def test_course_ranking(self):
        self.assertIn("OrderByDescending(c=>data.Enrollments.Count", SOURCE)
    def test_no_fabricated_revenue(self):
        self.assertIn("TuitionService.Summarize", SOURCE)
        self.assertIn("no implicit FX conversion", SOURCE)
    def test_bilingual_and_layout(self):
        self.assertIn('fa?"ثبت‌نام‌های شش ماه اخیر"', SOURCE)
        self.assertIn("voidLayoutDashboard(host)", SOURCE)

if __name__ == "__main__":
    unittest.main()
