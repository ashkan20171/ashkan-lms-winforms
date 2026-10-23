"""Portable static quality checks for the legacy .NET Framework project."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "AshkanLMS" / "AshkanLMS.csproj"
NS = {"msb": "http://schemas.microsoft.com/developer/msbuild/2003"}

class ProjectQualityTests(unittest.TestCase):
    def test_solution_and_project_exist(self):
        self.assertTrue((ROOT / "AshkanLMS.sln").is_file())
        self.assertTrue(PROJECT.is_file())

    def test_project_xml_is_well_formed(self):
        ET.parse(PROJECT)

    def test_target_framework_is_net48(self):
        root = ET.parse(PROJECT).getroot()
        frameworks = [e.text for e in root.findall(".//msb:TargetFrameworkVersion", NS)]
        self.assertIn("v4.8", frameworks)

    def test_compile_include_paths_exist(self):
        root = ET.parse(PROJECT).getroot()
        includes = [e.attrib.get("Include") for e in root.findall(".//msb:Compile", NS)]
        missing = [p for p in includes if p and not (PROJECT.parent / p.replace("\\", "/")).is_file()]
        self.assertEqual([], missing, f"Missing C# compile files: {missing}")

    def test_recruiter_documents_exist(self):
        for name in ("README.md", "ABOUT.md", "SECURITY.md", "ARCHITECTURE.md"):
            with self.subTest(file=name):
                self.assertTrue((ROOT / name).is_file(), f"Missing {name}")

    def test_sql_migrations_are_present(self):
        sql_dir = ROOT / "database"
        self.assertTrue(sql_dir.is_dir())
        self.assertGreaterEqual(len(list(sql_dir.glob("*.sql"))), 6)

    def test_stage_notes_exist(self):
        self.assertTrue((ROOT / "STAGE70-NOTES.md").is_file())

if __name__ == "__main__":
    unittest.main(verbosity=2)
