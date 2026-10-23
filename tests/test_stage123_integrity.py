import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SRC=(ROOT/'AshkanLMS/Services/DeploymentAuditOutbox.cs').read_text()
class Stage123Tests(unittest.TestCase):
    def test_sha256_checksum(self):
        self.assertIn('SHA256.Create()',SRC)
        self.assertIn('Digest(payload)',SRC)
    def test_legacy_compatibility(self):
        self.assertIn('if (fields.Length == 6) return true',SRC)
    def test_interprocess_lease(self):
        self.assertIn('FileShare.None',SRC)
        self.assertIn('using (lease)',SRC)
    def test_integrity_before_delivery(self):
        self.assertIn('if (!ValidEntry(file, fields))',SRC)
    def test_no_secret_dump(self):
        self.assertIn('ex.GetType().Name',SRC)
