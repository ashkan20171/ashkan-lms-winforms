import pathlib, unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Stage119Tests(unittest.TestCase):
    def test_sql_parameterization(self):
        src=(ROOT/'AshkanLMS/Services/SqlDeploymentAuditStore.cs').read_text()
        self.assertIn('SqlDbType.NVarChar',src)
        self.assertIn('AuthorizationService.RequireAccountAdmin()',src)
        self.assertIn('SqlDeploymentAuditStore',src)
    def test_schema(self):
        sql=(ROOT/'database/011_central_deployment_audit.sql').read_text()
        self.assertIn('CREATE TABLE dbo.LmsDeploymentAudit',sql)
        self.assertIn('CHECK (Outcome IN',sql)
    def test_project_registration(self):
        self.assertIn('Services\\SqlDeploymentAuditStore.cs',(ROOT/'AshkanLMS/AshkanLMS.csproj').read_text())
    def test_ui_filter(self):
        src=(ROOT/'AshkanLMS/UI/MainForm.Stage112.cs').read_text(encoding='utf-8-sig')
        self.assertIn('outcomeFilter.SelectedIndexChanged',src)
        self.assertIn('actionFilter.SelectedIndexChanged',src)
if __name__ == '__main__': unittest.main()
