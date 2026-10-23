import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Stage90(unittest.TestCase):
 def read(self,p): return (ROOT/p).read_text(encoding='utf-8-sig')
 def test_model(self): self.assertIn('class TuitionInstallment',self.read('AshkanLMS/Models/Entities.cs'))
 def test_state(self):
  s=self.read('AshkanLMS/Services/DataService.cs')
  self.assertIn('TuitionInstallments=TuitionInstallments',s)
  self.assertIn('state.TuitionInstallments',s)
 def test_validation(self): self.assertIn('ValidatePlan',self.read('AshkanLMS/Services/TuitionInstallmentService.cs'))
 def test_allocation(self): self.assertIn('Math.Min(row.Amount',self.read('AshkanLMS/Services/TuitionInstallmentService.cs'))
 def test_access(self): self.assertIn('key=="Installments"',self.read('AshkanLMS/UI/MainForm.cs'))
 def test_navigation(self): self.assertIn('ShowInstallmentManagement()',self.read('AshkanLMS/UI/MainForm.cs'))
 def test_compile_list(self): self.assertIn('MainForm.Stage90.cs',self.read('AshkanLMS/AshkanLMS.csproj'))
 def test_no_double_plan(self): self.assertIn('An installment plan already exists',self.read('AshkanLMS/Services/DataService.cs'))
if __name__=='__main__':unittest.main()
