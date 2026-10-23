# AshkanLMS

**A bilingual desktop Learning Management System built with C# and Windows Forms**

AshkanLMS is a portfolio-focused LMS project exploring desktop application architecture, role-aware workflows, operational diagnostics, and maintainable UI patterns on the .NET Framework. The project includes a bilingual Persian/English interface and a growing set of administrative and audit-support tools.

> **Project status:** Active portfolio project. The repository contains source code and validation/deployment helpers. A successful archive/XML check is not a substitute for a Windows build, runtime verification, or production security review; those checks are listed as pending unless explicitly reported otherwise.

## Highlights

- **C# / .NET Framework 4.8 / Windows Forms** — designed for Visual Studio on Windows.
- **Persian and English UI** — language-aware text, RTL/LTR layout direction, and localized operational screens.
- **LMS-oriented structure** — learner, instructor, academic operations, and administration concepts organized into UI, Services, Models, and Core layers.
- **Support & Diagnostics** — sanitized local event logging, diagnostic views, summary grouping, and CSV exports.
- **Audit review tooling** — review history, status summaries, and threshold-analysis components in the source tree.
- **Deployment helpers** — scripts for preflight checks, release packaging, installer preparation, and local configuration backup.
- **Privacy-conscious reporting** — diagnostic summaries are designed to export metadata rather than raw exception text or connection secrets.

## Technology

| Area | Technology |
|---|---|
| Language | C# |
| Desktop UI | Windows Forms |
| Runtime target | .NET Framework 4.8 |
| Data access / persistence | Local JSON mode and SQL Server-oriented components/migrations |
| IDE | Visual Studio 2022 recommended |
| Utilities | PowerShell, Python preflight helper, SQL scripts |

## Getting started

### Requirements

- Windows 10/11 or a compatible Windows environment
- Visual Studio 2022 with **.NET desktop development** workload
- .NET Framework 4.8 Developer Pack / targeting pack
- PowerShell for deployment helper scripts
- SQL Server only if you explicitly configure and use the relational persistence features

### Open and build

1. Extract the archive.
2. Open `AshkanLMS.sln` in Visual Studio.
3. Restore any required local prerequisites, if prompted.
4. Select `Debug` and `Any CPU`.
5. Run **Build → Rebuild Solution**.
6. Start the application with **F5** and verify login, navigation, language switching, and the modules you intend to demonstrate.

The default persistence mode and credentials may vary by project stage/configuration. Review the relevant configuration and login implementation before using a build. Do not assume demo credentials are suitable for deployment.

## Diagnostics and release checks

Run the portable project preflight helper from the repository root:

```powershell
python .\tools\preflight.py
```

For Windows release validation, use the included PowerShell helpers and inspect their output before packaging. Some scripts require Visual Studio/MSBuild or Inno Setup to be installed. Never distribute local configuration files containing credentials.

Useful locations:

- `tools/` — preflight, SQL checks, release packaging, and diagnostics helpers
- `database/` — SQL schema and migration scripts
- `AshkanLMS/UI/` — WinForms UI and stage-specific partial classes
- `AshkanLMS/Services/` — application services, persistence, authorization, audit, and diagnostics
- `ARCHITECTURE.md` — high-level architecture notes
- `ABOUT.md` — short portfolio description and recruiter-facing positioning
- `STAGE*-NOTES.md` — incremental implementation notes and known limitations

## Security and data handling

- Use a unique, strong administrator password outside development.
- Keep connection strings and credentials out of source control and release archives.
- Review authorization boundaries before exposing administrative and diagnostic views.
- Back up the database before applying SQL scripts; inspect migration order and target environment first.
- Treat diagnostic exports as operational data even when sanitized.
- Perform a real Windows build, runtime test, and security review before production use.

See [`SECURITY.md`](SECURITY.md) for reporting and handling security concerns.

## Quality status

The repository includes scripts and notes for static preflight and release preparation. These checks do not prove that the application compiles or works correctly. Validate the current stage in Visual Studio on Windows and test login, role permissions, Persian/English layout, data persistence, diagnostic log parsing, CSV output, and SQL migrations where applicable.

## Roadmap

- Add repeatable automated tests for log parsing, grouping, CSV formatting, and persistence boundaries.
- Establish a reproducible Windows CI build.
- Improve accessibility, keyboard navigation, and responsive layout behavior.
- Expand integration tests for SQL Server migrations and role-based access control.
- Document demo data and environment setup separately from production configuration.

## Repository metadata

**Suggested repository name:** `ashkan-lms-winforms`

**Suggested GitHub About:** `Bilingual C# WinForms Learning Management System with .NET Framework 4.8, role-aware workflows, audit tooling, and local diagnostics.`

**Topics:** `csharp`, `winforms`, `dotnet-framework`, `learning-management-system`, `lms`, `sql-server`, `desktop-application`, `localization`, `diagnostics`, `portfolio-project`

## Author

**Ashkan Motaei** — Software Support Specialist and Software Developer focused on C#, .NET, SQL Server, desktop applications, and maintainable business software.

This project is presented as a software engineering portfolio artifact. Features and validation status should be demonstrated from a working Windows build rather than inferred from documentation alone.
## Quality checks and CI

Run portable static checks from the repository root:

```powershell
python -m unittest discover -s tests -v
```

On Windows with Visual Studio Build Tools installed, validate required project files and rebuild Release:

```powershell
.\tools\validate-project.ps1 -RequireAllSqlScripts
.\tools\validate-project.ps1 -Build -Configuration Release -RequireAllSqlScripts
```

A Windows GitHub Actions workflow is provided at `.github/workflows/windows-quality.yml`. Its results become available after the repository is pushed and the workflow runs. Static checks are not a substitute for runtime, UI, or SQL Server integration testing.


## Stage 73 — Testable diagnostics

Diagnostic grouping and CSV serialization now live in a UI-independent C# service. See `STAGE73-NOTES.md`.

## Stage 74: executable C# diagnostics tests

Run `./tools/run-csharp-tests.ps1` in PowerShell on Windows with Visual Studio/MSBuild. The test project links the production diagnostic aggregation service and checks grouping, timestamp ranges, invalid types, and safe CSV output without external test packages. The Windows GitHub Actions workflow runs this suite.

## Stage 75 — Optional SQL Server integration smoke checks

Run read-only relational schema checks against a **dedicated test database** with `sqlcmd`:

```powershell
.\tools\test-sql-integration.ps1 -Server '.\SQLEXPRESS' -Database 'AshkanLmsTest'
```

See `STAGE75-NOTES.md` for prerequisites, limitations and security precautions. This is an opt-in smoke check, not a claim of full database integration coverage.


## Stage 76 — SQL behavioral integration tests

A transactional, rollback-only SQL Server test suite validates enrollment relationships, grades, uniqueness and foreign keys. **Run only against a disposable test database** with `tools/test-sql-behavior.ps1 -ConfirmDisposableDatabase`. See [Stage 76 notes](STAGE76-NOTES.md). These tests have not been executed against SQL Server in the authoring environment.

## Stage 77 — Authorization test matrix

A standalone C# .NET Framework 4.8 test harness links the actual authorization and session source files, with 28 assertions covering role permissions and missing identities. Run `./tools/run-authorization-tests.ps1` on Windows with MSBuild. The Windows quality workflow runs the harness after the diagnostic tests. This is a service-level authorization check, not a security certification or end-to-end authentication test.

## Stage 78 authentication tests
On Windows with Visual Studio MSBuild, run `./tools/run-authentication-tests.ps1` from PowerShell. The tests use a disposable isolated local folder and do not touch your real AshkanLMS account store.

## Stage 79: Session lifetime
A 30-minute inactivity timeout with UI-independent policy and 9 C# tests. Run `tools/run-session-tests.ps1` on Windows. See `STAGE79-NOTES.md`.

## Stage 80
Administrator Security Center now offers live filtered metrics and a CSV export of visible audit entries. See `STAGE80-NOTES.md`.

## Stage 81
Security Center visual upgrade: KPI tiles, seven-day event chart, anchored grid and bilingual captions. See `STAGE81-NOTES.md`.

## Stage 82 — Management dashboard
Six-month enrollment trends, top courses, and explicit no-financial-data state. See `STAGE82-NOTES.md`.

## Stage 83 — Tuition and payments

Persistent invoice/payment ledger, admin-only writes, currency-separated summaries, duplicate reference/overpayment validation, and a bilingual WinForms management page. See `STAGE83-NOTES.md` for scope and limitations.

## Stage 84 — Finance reporting
The administrator navigation exposes Tuition & Payments. Dashboard financial cards use actual stored invoice/payment records and separate IRR/EUR without exchange-rate assumptions. Tuition view supports search, due-date filtering and CSV export. See STAGE84-NOTES.md.

## Stage 85 — Financial reports
Administrator-only six-month payment reporting, debtor register, and invoice/receipt print preview. IRR and EUR are reported separately. See `STAGE85-NOTES.md`.

## Stage 86
Financial reports now include a custom six-month revenue bar chart and localized Persian/English invoice print preview. Use Windows Microsoft Print to PDF from the preview to save a PDF. See `STAGE86-NOTES.md` for limitations.


## Stage 87 — Multi-page invoice printing
Bilingual tuition invoice print preview now paginates long payment histories; see `STAGE87-NOTES.md`.


## Stage 89 — Financial UX
Tuition management now includes overdue-only filtering and bilingual color-coded payment status (Paid, Partial, Unpaid, Overdue), including CSV status export. See `STAGE89-NOTES.md`.


## Stage 90: Tuition Installment Plans
Administrator-only bilingual installment schedules (2–24 months), persisted with LMS state, oldest-due-first payment allocation, overdue tracking. See STAGE90-NOTES.md.


## Stage 91 — Installment Analytics
Administrator-only bilingual installment reporting with status/date/student filters, real payment allocation, per-currency KPIs and CSV export. See `STAGE91-NOTES.md`.

## Stage 92 — Installment reminders
Administrator-only reminder center with upcoming/overdue calculations, bilingual draft copying, local reviewed markers and CSV export. No messages are automatically sent. See `STAGE92-NOTES.md`.


Stage 94: administrator email reminder history, search/status filters, and duplicate-send confirmation. See STAGE94-NOTES.md.


### Stage 95
Notification Center with real installment alerts, unread counts, category filters and local per-user read state. See `STAGE95-NOTES.md`.


## Stage 96
Notification preferences, configurable due-date window and dashboard notification summary. See STAGE96-NOTES.md.


## Stage 97 — Dashboard UI and Priority Widgets
Responsive KPI layout, permission-aware installment alerts and academic activity pulse. See STAGE97-NOTES.md.

## Stage 98
Role-aware grouped sidebar, bilingual live navigation search and refreshed header. See STAGE98-NOTES.md.


## Stage 99: Appearance preferences
Light/dark theme selection with locally persisted preference and bilingual settings screen. See STAGE99-NOTES.md.

## Stage 100 — Stability pass

Stage 100 hardens empty-state navigation for student/course profiles, administrator-only tuition access, and CSV export safety. See [STAGE100-NOTES.md](STAGE100-NOTES.md) for validation and the Windows acceptance checklist. Structural Python tests are not a substitute for building the WinForms solution in Visual Studio.

### Stage 101 — Theme stabilization
A centralized post-navigation pass now harmonizes data grids and standard inputs with light/dark appearance preferences and Persian/English layout. See `STAGE101-NOTES.md` for scope and verification limitations.


## Stage 102 — Academic forms stabilization
Responsive academic grids, bilingual confirmation dialogs, stronger student/instructor email validation, and empty enrollment guidance. See `STAGE102-NOTES.md`.


## Stage 103
Student and instructor directories: searchable, paginated lists (20 records/page). See STAGE103-NOTES.md.

## Stage 104 — Academic profiles
Student and instructor grids now provide bilingual, read-only profile dialogs with enrollment/course histories. See `STAGE104-NOTES.md` for limitations and testing instructions.


Stage 105: Editable student/instructor phone and address profiles; validation, audit and persistence. See STAGE105-NOTES.md.


## Stage 106
Stable identity fields for enrollments and course instructors; ambiguity-safe legacy name lookup in profiles. See STAGE106-NOTES.md.


Stage 107: Academic identity hardening for attendance, grades and certificates; see STAGE107-NOTES.md.


## Stage 108
Academic attendance, grade and certificate forms now use stable student/course IDs; see STAGE108-NOTES.md.

## Stage 109: SQL Server academic identity audit
See `STAGE109-NOTES.md` for read-only orphan/duplicate/FK checks and optional guarded grade uniqueness. No automatic snapshot migration.


Stage 110: opt-in relational core migration preflight and transaction safeguards. See `STAGE110-NOTES.md`.


Stage 111: opt-in dependent academic migration; see STAGE111-NOTES.md.


## Stage 112 — Migration dashboard
Administration > Relational workspace > Migration dashboard offers a local preview, entity counts and an explicitly confirmed opt-in SQL import. See STAGE112-NOTES.md.


Stage 113: Read-only SQL target ID/relationship verification and admin CSV export. See STAGE113-NOTES.md.


Stage 114: read-only SQL Server field-level verification; see STAGE114-NOTES.md.


Stage 117: Admin-only read-only SQL Server deployment preflight (see STAGE117-NOTES.md).


Stage 118: admin-only local deployment audit history and CSV export. See STAGE118-NOTES.md.


Stage 119: Optional centralized deployment audit (see STAGE119-NOTES.md).


Stage 120: optional durable central audit outbox, idempotent SQL EventId migration; see STAGE120-NOTES.md.

Stage 121: Admin-only audit queue health inspector; see STAGE121-NOTES.md.

## Stage 122
Safe audit outbox quarantine and bounded retry. See `STAGE122-NOTES.md`.


Stage 123: SHA-256 queue integrity check and cross-process flush lease. See STAGE123-NOTES.md.
