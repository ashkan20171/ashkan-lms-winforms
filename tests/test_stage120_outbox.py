import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
class OutboxTests(unittest.TestCase):
    def test_outbox_compiled(self):
        self.assertIn('Services\\DeploymentAuditOutbox.cs', (ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
    def test_queue_before_send(self):
        s=(ROOT/'AshkanLMS/Services/DeploymentAuditTrail.cs').read_text()
        self.assertLess(s.index('DeploymentAuditOutbox.Enqueue(line)'),s.index('DeploymentAuditOutbox.FlushPending(25)'))
    def test_idempotent_insert(self):
        s=(ROOT/'AshkanLMS/Services/SqlDeploymentAuditStore.cs').read_text()
        self.assertIn('WITH (UPDLOCK, HOLDLOCK)',s)
        self.assertIn('EventId = @id',s)
    def test_unique_index(self):
        s=(ROOT/'database/012_central_audit_idempotency.sql').read_text()
        self.assertIn('CREATE UNIQUE INDEX',s)
    def test_explicit_retry(self):
        s=(ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text()
        self.assertIn('DeploymentAuditOutbox.FlushPending(100)',s)
