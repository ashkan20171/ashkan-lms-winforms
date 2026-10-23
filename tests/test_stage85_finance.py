from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
class Stage85Tests(unittest.TestCase):
 def test_report_service(self):
  t=(ROOT/'AshkanLMS/Services/TuitionReportingService.cs').read_text()
  for term in ('MonthlyPayments','Debtors','Outstanding','AddMonths(-5)','PaidAt<=asOf'):self.assertIn(term,t)
 def test_admin_guard(self):
  self.assertIn('AppSession.Role!="Administrator"',(ROOT/'AshkanLMS/UI/MainForm.Stage85.cs').read_text())
 def test_print_preview(self):
  t=(ROOT/'AshkanLMS/UI/MainForm.Stage85.cs').read_text();self.assertIn('PrintPreviewDialog',t);self.assertIn('PrintDocument',t)
 def test_wired(self):
  t=(ROOT/'AshkanLMS/UI/MainForm.cs').read_text(encoding='utf-8-sig');self.assertIn('ShowFinanceReports()',t)
 def test_compile_entries(self):
  t=(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text();self.assertIn('TuitionReportingService.cs',t);self.assertIn('MainForm.Stage85.cs',t)
