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
