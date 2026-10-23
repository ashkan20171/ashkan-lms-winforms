import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class FinanceContract(unittest.TestCase):
 def test_domain_models(self):
  text=(ROOT/'AshkanLMS/Models/Entities.cs').read_text();self.assertIn('class TuitionInvoice',text);self.assertIn('class TuitionPayment',text)
 def test_persistence(self):
  text=(ROOT/'AshkanLMS/Services/DataService.cs').read_text();self.assertIn('TuitionInvoices=TuitionInvoices',text);self.assertIn('TuitionPayments=TuitionPayments',text)
 def test_authorization(self):
  text=(ROOT/'AshkanLMS/Services/DataService.cs').read_text();self.assertIn('AuthorizationService.RequireAccountAdmin();',text)
 def test_overpayment(self):
  text=(ROOT/'AshkanLMS/Services/TuitionService.cs').read_text();self.assertIn('Payment exceeds invoice balance',text)
 def test_ui_and_project(self):
  self.assertIn('ShowTuitionManagement', (ROOT/'AshkanLMS/UI/MainForm.cs').read_text());self.assertIn('MainForm.Stage83.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
