import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
class Stage109SqlIdentityTests(unittest.TestCase):
    def test_readonly_audit(self):
        s=(ROOT/'database/009_academic_identity_audit.sql').read_text()
        self.assertIn('LmsEnrollments',s)
        self.assertIn('LmsAttendance',s)
        self.assertIn('LmsGrades',s)
        self.assertIn('LmsCertificates',s)
        self.assertIn('is_not_trusted',s)
        self.assertNotIn('DELETE FROM',s.upper())
        self.assertNotIn('UPDATE ',s.upper())
    def test_guarded_constraint(self):
        s=(ROOT/'database/010_academic_identity_constraints.sql').read_text()
        self.assertIn('HAVING COUNT_BIG(*)>1',s)
        self.assertIn('CREATE UNIQUE INDEX',s)
        self.assertIn('BEGIN TRANSACTION',s)
    def test_documents_snapshot_boundary(self):
        s=(ROOT/'STAGE109-NOTES.md').read_text()
        self.assertIn('snapshot',s.lower())
        self.assertIn('No live SQL Server execution',s)
