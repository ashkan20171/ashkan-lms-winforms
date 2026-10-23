import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Stage76Contracts(unittest.TestCase):
    def test_transaction_always_rolled_back(self):
        s=(ROOT/'database/008_transactional_behavior_tests.sql').read_text()
        self.assertIn('ROLLBACK TRANSACTION;',s)
        self.assertNotIn('COMMIT TRANSACTION',s)
        self.assertIn('BEGIN CATCH',s)
    def test_constraint_cases(self):
        s=(ROOT/'database/008_transactional_behavior_tests.sql').read_text()
        for phrase in ('Duplicate enrollment was accepted','Out-of-range grade was accepted','Missing student FK was accepted','Valid grade persistence failed'):
            self.assertIn(phrase,s)
    def test_opt_in_runner(self):
        s=(ROOT/'tools/test-sql-behavior.ps1').read_text()
        for phrase in ('ConfirmDisposableDatabase',"'-E'","'-b'",'test|dev|sandbox'):
            self.assertIn(phrase,s)
