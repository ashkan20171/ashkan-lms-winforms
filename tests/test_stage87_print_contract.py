import pathlib
import unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class Stage87Tests(unittest.TestCase):
 def test_print_service_in_project(self):
  self.assertIn('Services\\InvoicePrintService.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
 def test_pagination(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage85.cs').read_text()
  self.assertIn('pe.HasMorePages=nextLine<lines.Count',s)
  self.assertIn('document.BeginPrint',s)
 def test_invoice_payment_details(self):
  s=(ROOT/'AshkanLMS/Services/InvoicePrintService.cs').read_text()
  self.assertIn('OrderBy(p=>p.PaidAt)',s)
  self.assertIn('invoice.Amount-paid.Sum',s)
 def test_pdf_claim(self):
  self.assertIn('Microsoft Print to PDF',(ROOT/'STAGE87-NOTES.md').read_text())
if __name__=='__main__':unittest.main()
