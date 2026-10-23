import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class SqlIntegrationContractTests(unittest.TestCase):
    def test_sql_smoke_is_read_only(self):
        sql = (ROOT / 'database/007_integration_smoke.sql').read_text(encoding='utf-8')
        self.assertIn('sys.foreign_keys', sql)
        self.assertIn('sys.check_constraints', sql)
        self.assertIn('THROW 51000', sql)
        for token in ('DROP TABLE', 'DELETE FROM dbo.', 'UPDATE dbo.', 'ALTER TABLE', 'CREATE TABLE dbo.'):
            self.assertNotIn(token, sql.upper())

    def test_sql_runner_is_opt_in(self):
        ps = (ROOT / 'tools/test-sql-integration.ps1').read_text(encoding='utf-8')
        self.assertIn('[Parameter(Mandatory=$true)][string]$Database', ps)
        self.assertIn("'-E'", ps)
        self.assertIn("'-b'", ps)
        self.assertNotIn('Password=', ps)

if __name__ == '__main__':
    unittest.main()
