import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class BrandingTests(unittest.TestCase):
 def test_branding_persisted(self):
  s=(ROOT/'AshkanLMS/Services/InvoiceBrandingService.cs').read_text(encoding='utf8')
  self.assertIn('SpecialFolder.LocalApplicationData',s)
  self.assertIn('XmlSerializer',s)
  self.assertIn('File.Replace',s)
 def test_admin_only(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage88.cs').read_text(encoding='utf8')
  self.assertIn('AppSession.Role!="Administrator"',s)
 def test_print_uses_branding(self):
  s=(ROOT/'AshkanLMS/UI/MainForm.Stage85.cs').read_text(encoding='utf8')
  self.assertIn('InvoiceBrandingService.Load()',s)
  self.assertIn('EditInvoiceBranding()',s)
  self.assertIn('DrawImage',s)
 def test_project_includes_files(self):
  s=(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text(encoding='utf8')
  self.assertIn('MainForm.Stage88.cs',s)
  self.assertIn('InvoiceBrandingService.cs',s)
if __name__=='__main__':unittest.main()
