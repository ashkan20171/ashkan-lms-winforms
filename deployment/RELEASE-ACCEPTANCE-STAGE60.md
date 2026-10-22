# Stage 60 — Windows acceptance checklist

## Before testing
- Use a disposable Windows test machine or VM and a non-production database.
- Install .NET Framework 4.8 runtime, Visual Studio Build Tools targeting pack, and Inno Setup 6.
- Record previous version and back up **all actual LMS user data** separately (JSON files or SQL Server backup). `backup-local-config.ps1` backs up **configuration only**.

## Test matrix (manual; not yet executed)
| Scenario | Expected result | Actual |
|---|---|---|
| Clean build | MSBuild Release succeeds | Not tested |
| Portable package | EXE/config/SQL scripts present | Not tested |
| Clean install | Setup installs and shortcuts work | Not tested |
| First run JSON | Login and data persistence work | Not tested |
| Close/reopen | Saved data remains intact | Not tested |
| In-place upgrade | Existing configuration and data remain | Not tested |
| Uninstall | No unexpected data deletion | Not tested |
| SQL opt-in | Existing SQL connection and schema verified read-only | Not tested |
| Persian/English | RTL/LTR layout and labels correct | Not tested |
| Restricted account | Non-admin operations blocked | Not tested |

## Procedure
1. Run `tools/release-check.ps1 -Build`, `tools/package-release.ps1`, then `tools/build-installer.ps1`.
2. Run `tools/test-release.ps1 -RequireSqlScripts -CheckInstaller`.
3. On the test VM, run a clean install and test JSON persistence.
4. Back up real data **using the appropriate storage-specific procedure** before upgrade. For config only: `tools/backup-local-config.ps1 -InstallDir 'C:\Program Files\AshkanLMS' -BackupDir 'C:\Temp\AshkanLMS-config-backup-001'`.
5. Upgrade over existing installation and verify config, login and data.
6. Record observed outcomes, screenshots, logs, Windows edition, .NET release and installer hash.

## Safety
- Never assume `AshkanLMS.exe.config` contains the application's user records.
- SQL migrations are never run automatically.
- Do not include sensitive backups in release ZIPs or public GitHub repositories.
- Do not declare production readiness until all acceptance tests pass on Windows.
