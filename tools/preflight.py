#!/usr/bin/env python3
"""Static, offline preflight for the classic .NET Framework 4.8 solution.
Not a replacement for Visual Studio compilation or SQL Server integration tests.
"""
from pathlib import Path
import sys, xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
project=root/'AshkanLMS'/'AshkanLMS.csproj'
errors=[]
try:
    doc=ET.parse(project)
except ET.ParseError as e:
    sys.exit('FAIL: invalid csproj XML: '+str(e))
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
files=[e.attrib['Include'].replace('\\','/') for e in doc.findall('.//m:Compile',ns)]
if len(files)!=len(set(files)): errors.append('Duplicate Compile Include entries')
for rel in files:
    if not (project.parent/rel).is_file(): errors.append('Missing Compile Include: '+rel)
if not any('MainForm.Stage50' in x for x in files):
    pass # Stage 50 modifies existing partial files rather than adding a new one.
for rel in ['database/001_relational_schema.sql','database/002_rowversion_concurrency.sql','database/003_relational_audit.sql','database/004_audit_reviews.sql','database/005_audit_review_history.sql','database/006_deletion_threshold_rules.sql']:
    if not (root/rel).is_file(): errors.append('Missing SQL migration: '+rel)
for rel in files:
    if rel.endswith('.cs'):
        data=(project.parent/rel).read_text(encoding='utf-8-sig')
        if '<<<<<<<' in data or '>>>>>>>' in data: errors.append('Merge marker in '+rel)
print('Checked',len(files),'C# compile entries and 6 SQL migration files.')
if errors:
    for issue in errors: print('FAIL:',issue)
    sys.exit(1)
print('PASS: static file/manifest checks; Windows compilation and database tests NOT run.')
