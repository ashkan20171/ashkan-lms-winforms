import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Stage106IdentityTests(unittest.TestCase):
 def test_identity_fields(self):
  s=(ROOT/'AshkanLMS/Models/Entities.cs').read_text(); self.assertIn('public int StudentId{get;set;}',s); self.assertIn('public int CourseId{get;set;}',s); self.assertIn('public int InstructorId{get;set;}',s)
 def test_safe_legacy_fallback(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage104.cs').read_text(); self.assertIn('x.StudentId == id',s); self.assertIn('x.InstructorId == id',s); self.assertIn('Count(i =>',s)
 def test_migration_opt_in(self):
  s=(ROOT/'AshkanLMS/Services/AcademicIdentityService.cs').read_text(); self.assertIn('LinkUnambiguousLegacyRecords',s); self.assertIn('matches.Count == 1',s)
 def test_project_reference(self):
  self.assertIn('AcademicIdentityService.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
