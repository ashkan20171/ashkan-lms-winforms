import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
S=(ROOT/'AshkanLMS/Services/RelationalMigrationService.cs').read_text()
class MigrationStage110Tests(unittest.TestCase):
 def test_preflight(self): self.assertIn('MigrationPreview PreviewCore(LmsState state)',S)
 def test_explicit_student_id(self): self.assertIn('int studentId = e.StudentId, courseId = e.CourseId;',S)
 def test_explicit_instructor_id(self): self.assertIn('int id = c.InstructorId;',S)
 def test_ambiguous_names_fail(self): self.assertIn('matches.Count != 1',S)
 def test_preflight_before_connection(self): self.assertLess(S.index('var preview = PreviewCore(state)'),S.index('connection.Open()'))
 def test_transaction(self): self.assertIn('IsolationLevel.Serializable',S)
 def test_rollback(self): self.assertIn('transaction.Rollback()',S)
 def test_all_tables_empty(self): self.assertIn('dbo.LmsCertificates',S)
 def test_parameterized(self): self.assertIn('command.Parameters.AddRange(parameters)',S)
 def test_no_auto_migration(self): self.assertIn('internal void ImportCore',S)
if __name__=='__main__': unittest.main()
