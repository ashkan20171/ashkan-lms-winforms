<div align="center">

# 🎓 AshkanLMS

### A Bilingual Learning Management System for Desktop

**C# · .NET Framework 4.8 · Windows Forms · SQL Server · Persian / English**

[![C#](https://img.shields.io/badge/C%23-Desktop%20Engineering-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-5C2D91?style=for-the-badge)](https://learn.microsoft.com/dotnet/framework/)
[![Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms-0078D4?style=for-the-badge)](https://learn.microsoft.com/dotnet/desktop/winforms/)
[![SQL Server](https://img.shields.io/badge/Data-SQL%20Server%20%2B%20Local-CC2927?style=for-the-badge)](https://learn.microsoft.com/sql/)

*Desktop software engineering • Academic operations • Localization • Data integrity • Operational reliability*

[Overview](#-overview) · [Features](#-feature-highlights) · [Architecture](#-architecture) · [Getting Started](#-getting-started) · [Engineering Quality](#-engineering-quality) · [Roadmap](#-roadmap)

</div>

---

## ✨ Overview

**AshkanLMS** is a C# desktop Learning Management System designed to explore the engineering challenges behind real-world education administration: student and instructor records, course enrollment, academic workflows, tuition-related operations, bilingual user interfaces, and reliable data handling.

The application is built with **Windows Forms on .NET Framework 4.8**, using an organized codebase with UI, service, model, and infrastructure responsibilities. It combines local persistence with **SQL Server-oriented relational migration, verification, and audit tooling**.

> **Project maturity:** Actively developed portfolio project. Features listed below are represented in the source code, but not all workflows have been validated end-to-end in a Windows environment. SQL migration and central audit capabilities require explicit configuration and controlled testing; they are not enabled automatically.

## 🚀 Feature Highlights

| Area | Capabilities represented in the project |
|:--|:--|
| 👥 **People & roles** | Student and instructor records, authentication and role-aware administrative workflows |
| 📚 **Academic management** | Courses, enrollment, attendance, assignments, examinations, grades, and certificates |
| 🆔 **Academic data integrity** | Stable-identifier models, identity resolution, relationship validation, and legacy-data ambiguity checks |
| 💳 **Tuition operations** | Tuition, installment tracking, reporting, invoicing and reminder-related services |
| 🌍 **Bilingual experience** | Persian (RTL) and English (LTR) interface support, with localization-aware layouts |
| 📊 **Reporting & diagnostics** | CSV exports, diagnostic aggregation, operational summaries and reporting helpers |
| 🛡️ **Administration & audit** | Authorization boundaries, audit trail, SQL deployment preflight, migration verification and discrepancy reporting |
| 🔄 **Audit synchronization** | Optional central SQL audit storage, local outbox, idempotent event identifiers, quarantine and integrity checks |
| 🧰 **Deployment tooling** | PowerShell scripts, SQL scripts, validation helpers and Windows-oriented CI configuration |

### Selected engineering challenges

**Identity over display names.** Academic relationships increasingly use stable numeric IDs, with conservative handling of ambiguous historical name-based records.

**Safe migration practices.** The project includes opt-in SQL Server migration services, preflight checks, transactional handling, discrepancy reports and readiness assessments. Migration should be trialed against an isolated database and verified before operational use.

**Resilient audit delivery.** The audit subsystem includes a local event outbox, unique event identifiers, retry/quarantine behavior and SHA-256 checks for accidental corruption. **SHA-256 is not a cryptographic signature and does not prevent deliberate tampering.**

## 🏗️ Architecture

```text
AshkanLMS/
├── UI/              Windows Forms, navigation, dialogs, localization
├── Services/        Academic use cases, authorization, reporting,
│                   persistence, migration, diagnostics and audit
├── Models/          Students, instructors, courses and academic entities
├── Core/            Cross-cutting application state and presentation helpers
└── AshkanLMS.csproj

database/             SQL Server schema and migration scripts
tools/                PowerShell and Python validation/release helpers
tests/                Structural and selected C# test projects
.github/workflows/    Windows-oriented quality automation
ARCHITECTURE.md       Architecture notes
SECURITY.md           Security guidance
STAGE*-NOTES.md       Incremental implementation history
```

### Technology stack

| Layer | Technology |
|---|---|
| Language | C# |
| Desktop framework | .NET Framework 4.8 |
| UI | Windows Forms |
| Data | Local JSON persistence; optional SQL Server components |
| Database access | ADO.NET / SQL Server-oriented repository and migration services |
| Testing | Python structural tests; selected C# test projects |
| Tooling | Visual Studio 2022, PowerShell, Python, SQL scripts |
| CI | Windows GitHub Actions workflow included in repository |

## 🌐 Localization & UX

The project supports **Persian (right-to-left)** and **English (left-to-right)** experiences. Its UI design emphasizes clear administrative navigation, consistent visual hierarchy and readable operational screens. Language support should be tested across each screen and dialog rather than assumed from the presence of a language toggle.

> **Screenshots:** Add verified screenshots from a successful Windows run under `docs/screenshots/` and embed them here. Avoid mockups presented as running software.

## 🖥️ Getting Started

### Prerequisites

- Windows 10/11
- Visual Studio 2022 with **.NET desktop development** workload
- .NET Framework 4.8 Developer Pack / targeting pack
- Python (for structural tests)
- SQL Server / SQL Server Express **only for optional relational features**

### Build and run

```powershell
# 1. Clone your published repository
git clone https://github.com/ashkan20171/ashkan-lms-dotnet-winforms.git
cd ashkan-lms-dotnet-winforms

# 2. Run portable structural tests
python -m unittest discover -s tests -v

# 3. Optional project preflight
python .\tools\preflight.py
```

Open **`AshkanLMS.sln`** in Visual Studio, select the intended build configuration, run **Build → Rebuild Solution**, and launch the application with **F5**.

**Important:** Review configuration, login setup, permissions and persistence mode before running. Do not commit passwords, private connection strings or local production data. The GitHub URL above is the *suggested future repository URL*, not a claim that it has already been published.

### Optional SQL Server setup

The repository contains SQL scripts and optional SQL-oriented services. Use a **disposable test database**, take backups, inspect script dependencies, and apply scripts in the documented order. Central audit and migration functionality require explicit configuration; the application does **not** automatically switch its primary persistence to SQL Server.

## 🧪 Engineering Quality

The repository includes structural tests, selected C# test projects, preflight scripts and a Windows CI workflow. These provide useful development checks but **do not independently establish a successful current-stage Windows build, working UI, or production readiness**.

Recommended acceptance checks:

1. Rebuild the solution in Visual Studio on Windows.
2. Verify login, roles and administrative authorization.
3. Exercise student/course/enrollment and academic workflows.
4. Validate Persian/English text and RTL/LTR behavior on every major screen.
5. Test persistence, SQL scripts and migration against isolated test data.
6. Test backup/restore and audit synchronization under connection failure.
7. Inspect exported CSV files and ensure no secrets are leaked.

## 🔐 Security & Data Responsibility

- Apply least-privilege access to administrator and SQL accounts.
- Keep secrets out of the repository and diagnostic exports.
- Verify backup **restoration**, not just backup history.
- Review the limitations of local audit files and non-keyed SHA-256 hashes.
- Treat this as an engineering portfolio project until Windows runtime, integration and security acceptance checks are complete.

See [`SECURITY.md`](SECURITY.md) and the stage notes for implementation details.

## 🗺️ Roadmap

- [ ] Reproducible successful Windows build for the latest stage
- [ ] End-to-end automated tests for critical academic flows
- [ ] SQL Server migration tests and restore drills
- [ ] Stronger audit integrity (keyed authentication and secure key management)
- [ ] More complete localization and accessibility validation
- [ ] Verified screenshots, demo walkthrough and sample dataset

## 👨‍💻 Author

**Ashkan Motaei**  
Software Developer | C#/.NET | SQL Server | Desktop & Business Applications

[GitHub](https://github.com/ashkan20171)

*Built as a software engineering portfolio project with a focus on maintainability, business workflows, localization and operational reliability.*

---

<div align="center">
  <sub>Designed to demonstrate practical C#/.NET engineering skills to technical teams and recruiters across Germany and Europe.</sub>
</div>
