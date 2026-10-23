import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class Stage104Profiles(unittest.TestCase):
    def test_new_file_included_in_project(self):
        self.assertIn('MainForm.Stage104.cs', (ROOT/'AshkanLMS/AshkanLMS.csproj').read_text(encoding='utf-8'))
    def test_student_profile_wired(self):
        self.assertIn('AttachAcademicProfiles104(c,g,true)', (ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8'))
    def test_instructor_profile_wired(self):
        self.assertIn('AttachAcademicProfiles104(card,grid,false)', (ROOT/'AshkanLMS/UI/MainForm.Stage17.cs').read_text(encoding='utf-8'))
    def test_empty_selection_guard(self):
        self.assertIn('grid.CurrentRow == null', (ROOT/'AshkanLMS/UI/MainForm.Stage104.cs').read_text(encoding='utf-8'))
    def test_no_invented_contact_fields(self):
        self.assertIn('Contact details can be edited.', (ROOT/'AshkanLMS/UI/MainForm.Stage104.cs').read_text(encoding='utf-8'))
    def test_related_enrollments_and_courses(self):
        source=(ROOT/'AshkanLMS/UI/MainForm.Stage104.cs').read_text(encoding='utf-8')
        self.assertIn('data.Enrollments.Where',source)
        self.assertIn('data.Courses.Where',source)

if __name__ == '__main__': unittest.main()
