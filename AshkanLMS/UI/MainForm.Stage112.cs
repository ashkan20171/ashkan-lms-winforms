using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Explicit admin-only migration dashboard. Preview is local/read-only.
        private void ShowMigrationDashboard112()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            using (var dialog = new Form {
                Text = fa ? "داشبورد مهاجرت کنترل‌شده" : "Controlled migration dashboard",
                Width = 790, Height = 650, MinimumSize = new Size(620, 520),
                StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Canvas,
                RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = fa
            })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 5 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
                dialog.Controls.Add(layout);
                var heading = new Label { Dock = DockStyle.Fill, Font = Theme.Font(15, FontStyle.Bold), ForeColor = Theme.Text,
                    Text = fa ? "پیش‌نمایش و انتقال به دیتابیس رابطه‌ای" : "Preview and import into relational SQL Server" };
                layout.Controls.Add(heading, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Theme.Card,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                grid.Columns.Add("Entity", fa ? "موجودیت" : "Entity");
                grid.Columns.Add("Count", fa ? "تعداد" : "Count");
                layout.Controls.Add(grid, 0, 1);
                var notice = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted,
                    Text = fa ? "پیش‌نمایش فقط داده‌های محلی را بررسی می‌کند و به SQL متصل نمی‌شود." :
                    "Preview validates the current snapshot locally; it does not connect to SQL Server." };
                layout.Controls.Add(notice, 0, 2);
                var backup = new CheckBox { Dock = DockStyle.Fill, ForeColor = Theme.Text,
                    Text = fa ? "از منبع و مقصد نسخه پشتیبان معتبر گرفته‌ام و مقصد اختصاصی و خالی است." :
                    "I have verified source/target backups and a dedicated empty target." };
                layout.Controls.Add(backup, 0, 3);
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
                layout.Controls.Add(actions, 0, 4);
                var previewButton = Theme.Button(fa ? "بررسی اطلاعات" : "Validate preview");
                var importButton = Theme.Button(fa ? "شروع انتقال" : "Import to SQL Server");
                var verifyButton = Theme.Button(fa ? "بررسی مقصد و گزارش CSV" : "Verify target / Export CSV");
                importButton.Enabled = false;
                actions.Controls.Add(previewButton); actions.Controls.Add(importButton); actions.Controls.Add(verifyButton);
                var preflightButton = Theme.Button(fa ? "بررسی پیش از استقرار" : "SQL deployment preflight");
                actions.Controls.Add(preflightButton);
                var auditButton = Theme.Button(fa ? "سوابق ممیزی" : "Deployment audit history");
                actions.Controls.Add(auditButton);
                var retryAuditButton = Theme.Button(fa ? "همگام‌سازی ممیزی" : "Sync pending audit");
                actions.Controls.Add(retryAuditButton);
                var inspectQueueButton = Theme.Button(fa ? "بررسی صف ممیزی" : "Inspect audit queue");
                actions.Controls.Add(inspectQueueButton);
                inspectQueueButton.Click += (sender, args) => {
                    try {
                        AuthorizationService.RequireAccountAdmin();
                        var rows = DeploymentAuditOutbox.InspectPending(500)
                            .Concat(DeploymentAuditOutbox.InspectQuarantine(500)).ToArray();
                        using (var viewer = new Form { Text = fa ? "وضعیت صف ممیزی" : "Audit queue health",
                            Width = 650, Height = 460, StartPosition = FormStartPosition.CenterParent,
                            RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = fa }) {
                            var list = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                            list.Columns.Add("File", fa ? "شناسه فایل" : "Queue file");
                            list.Columns.Add("State", fa ? "وضعیت" : "Status");
                            foreach (var row in rows) {
                                var parts = row.Split('\t');
                                if (parts.Length == 2) list.Rows.Add(parts[0], parts[1]);
                            }
                            viewer.Controls.Add(list);
                            viewer.ShowDialog(dialog);
                        }
                    } catch (Exception ex) {
                        MessageBox.Show(dialog, ex.GetType().Name, "Audit queue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                retryAuditButton.Click += (sender, args) => {
                    try {
                        AuthorizationService.RequireAccountAdmin();
                        var sent = DeploymentAuditOutbox.FlushPending(100);
                        var pending = DeploymentAuditOutbox.PendingCount;
                        var quarantined = DeploymentAuditOutbox.QuarantinedCount;
                        var message = (fa ? "ارسال شده: " : "Sent: ") + sent +
                            (fa ? " | در انتظار: " : " | Pending: ") + pending +
                            (fa ? " | قرنطینه: " : " | Quarantined: ") + quarantined;
                        if (!string.IsNullOrEmpty(DeploymentAuditOutbox.LastError))
                            message += "\n" + DeploymentAuditOutbox.LastError;
                        MessageBox.Show(dialog, message, fa ? "وضعیت همگام‌سازی" : "Audit sync status");
                    } catch (Exception ex) {
                        MessageBox.Show(dialog, ex.GetType().Name, "Audit sync", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                auditButton.Click += (sender, args) => {
                    try {
                        AuthorizationService.RequireAccountAdmin();
                        var entries = SqlDeploymentAuditStore.IsEnabled
                            ? SqlDeploymentAuditStore.ReadRecent() : DeploymentAuditTrail.ReadRecent();
                        using (var history = new Form { Text = fa ? "سوابق ممیزی استقرار" : "Deployment audit history",
                            Width = 920, Height = 520, StartPosition = FormStartPosition.CenterParent }) {
                            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill };
                            var actionFilter = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
                            actionFilter.Items.Add(fa ? "همه عملیات" : "All actions");
                            foreach (var action in entries.Select(x => x.Split('\t')).Where(x => x.Length == 5)
                                .Select(x => x[2]).Distinct().OrderBy(x => x)) actionFilter.Items.Add(action);
                            actionFilter.SelectedIndex = 0;
                            var outcomeFilter = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
                            outcomeFilter.Items.Add(fa ? "همه نتایج" : "All outcomes");
                            outcomeFilter.Items.Add("SUCCESS"); outcomeFilter.Items.Add("FAILED");
                            outcomeFilter.SelectedIndex = 0;
                            filters.Controls.Add(actionFilter); filters.Controls.Add(outcomeFilter);
                            panel.Controls.Add(filters, 0, 0);
                            var table = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                                AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                            foreach (var column in new [] { "UTC", "Admin", "Action", "Outcome", "Summary" }) table.Columns.Add(column, column);
                            Action refresh = () => {
                                table.Rows.Clear();
                                foreach (var entry in entries) {
                                    var parts = entry.Split('\t');
                                    if (parts.Length != 5) continue;
                                    if (actionFilter.SelectedIndex > 0 && parts[2] != actionFilter.SelectedItem.ToString()) continue;
                                    if (outcomeFilter.SelectedIndex > 0 && parts[3] != outcomeFilter.SelectedItem.ToString()) continue;
                                    table.Rows.Add(parts);
                                }
                            };
                            actionFilter.SelectedIndexChanged += (s2, e2) => refresh();
                            outcomeFilter.SelectedIndexChanged += (s2, e2) => refresh();
                            refresh();
                            var export = Theme.Button(fa ? "خروجی CSV" : "Export CSV");
                            export.Click += (s2, e2) => {
                                using (var save = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "AshkanLMS-Deployment-Audit.csv" })
                                    if (save.ShowDialog(history) == DialogResult.OK)
                                        File.WriteAllText(save.FileName, DeploymentAuditTrail.ToCsv(table.Rows.Cast<DataGridViewRow>().Select(r =>
                                            string.Join("\t", r.Cells.Cast<DataGridViewCell>().Select(c => Convert.ToString(c.Value) ?? "")))), new UTF8Encoding(true));
                            };
                            panel.Controls.Add(table, 0, 1); panel.Controls.Add(export, 0, 2);
                            history.Controls.Add(panel); history.ShowDialog(dialog);
                        }
                    } catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Audit history", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                preflightButton.Click += (sender, args) => {
                    try {
                        AuthorizationService.RequireAccountAdmin();
                        var connection = ConfigurationManager.ConnectionStrings["AshkanLMS"];
                        if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                            throw new InvalidOperationException("Missing AshkanLMS SQL Server connection string.");
                        var checks = SqlDeploymentPreflight.Run(connection.ConnectionString);
                        DeploymentAuditTrail.Record("SQL_PREFLIGHT", checks.All(x => x.Passed), "Checks: " + checks.Count + "; failed: " + checks.Count(x => !x.Passed));
                        var passed = checks.All(x => x.Passed);
                        MessageBox.Show(dialog, SqlDeploymentPreflight.Format(checks),
                            fa ? "چک‌لیست SQL Server" : "SQL Server deployment preflight",
                            MessageBoxButtons.OK, passed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        notice.Text = passed ? (fa ? "بررسی‌های خودکار موفق؛ تأیید دستی پشتیبان و تست بازیابی الزامی است." :
                            "Automated checks passed; backup restore and build must still be verified.") :
                            (fa ? "برخی پیش‌نیازهای SQL Server برقرار نیستند." : "SQL Server preflight found blockers.");
                    } catch (Exception ex) {
                        MessageBox.Show(dialog, ex.Message, "SQL Server preflight", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                RelationalMigrationService.MigrationPreview verified = null;
                previewButton.Click += (sender, args) => {
                    verified = null; importButton.Enabled = false; grid.Rows.Clear();
                    try {
                        var connection = ConfigurationManager.ConnectionStrings["AshkanLMS"];
                        if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                            throw new InvalidOperationException("Missing AshkanLMS SQL Server connection string.");
                        var migration = new RelationalMigrationService(connection.ConnectionString);
                        verified = migration.PreviewCore(data.SnapshotForMigration112());
                        DeploymentAuditTrail.Record("MIGRATION_PREVIEW", true, "Local snapshot validated");
                        grid.Rows.Add(fa ? "دانشجویان" : "Students", verified.Students);
                        grid.Rows.Add(fa ? "مدرسین" : "Instructors", verified.Instructors);
                        grid.Rows.Add(fa ? "دوره‌ها" : "Courses", verified.Courses);
                        grid.Rows.Add(fa ? "ثبت‌نام‌ها" : "Enrollments", verified.Enrollments);
                        grid.Rows.Add(fa ? "حضور و غیاب" : "Attendance", verified.Attendance);
                        grid.Rows.Add(fa ? "نمرات" : "Grades", verified.Grades);
                        grid.Rows.Add(fa ? "گواهینامه‌ها" : "Certificates", verified.Certificates);
                        notice.Text = fa ? "بررسی محلی موفق بود؛ وضعیت مقصد هنوز بررسی نشده است." :
                            "Local validation passed; target SQL Server has not yet been checked.";
                        importButton.Enabled = true;
                    } catch (Exception ex) {
                        notice.Text = fa ? "بررسی ناموفق: " + ex.Message : "Validation failed: " + ex.Message;
                    }
                };
                importButton.Click += (sender, args) => {
                    if (verified == null || !backup.Checked) {
                        MessageBox.Show(dialog, fa ? "ابتدا پیش‌نمایش و تأیید پشتیبان‌گیری لازم است." :
                            "Validate the preview and confirm verified backups first."); return;
                    }
                    // Explicit typed confirmation; migration service independently validates again.
                    using (var confirmation = new Form { Text = "CONFIRM IMPORT", Width = 470, Height = 195,
                        StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Canvas }) {
                        var prompt = new Label { Dock = DockStyle.Top, Height = 62, ForeColor = Theme.Text,
                            Text = fa ? "برای انتقال به دیتابیس اختصاصی و خالی، عبارت IMPORT را وارد کنید." :
                            "Type IMPORT to write to the dedicated empty SQL Server target." };
                        var typed = new TextBox { Dock = DockStyle.Top };
                        var accept = Theme.Button(fa ? "تأیید نهایی" : "Confirm import");
                        accept.Dock = DockStyle.Bottom;
                        accept.Click += (s, e) => { if (typed.Text == "IMPORT") confirmation.DialogResult = DialogResult.OK; };
                        confirmation.Controls.Add(accept); confirmation.Controls.Add(typed); confirmation.Controls.Add(prompt);
                        if (confirmation.ShowDialog(dialog) != DialogResult.OK) return;
                    }
                    importButton.Enabled = false; verified = null;
                    try {
                        AuthorizationService.RequireAccountAdmin();
                        var connection = ConfigurationManager.ConnectionStrings["AshkanLMS"];
                        if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                            throw new InvalidOperationException("Missing SQL Server connection string.");
                        new RelationalMigrationService(connection.ConnectionString).ImportCore(data.SnapshotForMigration112());
                        notice.Text = fa ? "انتقال تکمیل شد. صحت داده‌های مقصد را جداگانه بررسی کنید." :
                            "Import completed. Independently verify target data before cutover.";
                        DeploymentAuditTrail.Record("MIGRATION_IMPORT", true, "Import transaction completed");
                        MessageBox.Show(dialog, notice.Text);
                    } catch (Exception ex) {
                        DeploymentAuditTrail.Record("MIGRATION_IMPORT", false, "Import operation failed");
                        notice.Text = fa ? "انتقال ناموفق: " + ex.Message : "Import failed: " + ex.Message;
                        MessageBox.Show(dialog, notice.Text, "SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                verifyButton.Click += (sender, args) => {
                    try {
                        AuthorizationService.RequireAccountAdmin();
                        var connection = ConfigurationManager.ConnectionStrings["AshkanLMS"];
                        if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                            throw new InvalidOperationException("Missing SQL Server connection string.");
                        var result = new RelationalMigrationService(connection.ConnectionString).VerifyTarget(data.SnapshotForMigration112());
                        var readiness = result.AssessReadiness();
                        DeploymentAuditTrail.Record("MIGRATION_VERIFY", readiness.Ready, "Verified; ready=" + readiness.Ready);
                        MessageBox.Show(dialog, readiness.ToReport(),
                            fa ? "گزارش آمادگی مهاجرت" : "Migration readiness report",
                            MessageBoxButtons.OK, readiness.Ready ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                        grid.Rows.Clear();
                        foreach (var row in result.Rows)
                            grid.Rows.Add(row.Name, row.Expected + " / " + row.Actual + " | -" + row.Missing + " +" + row.Extra + " !" + row.Mismatched);
                        if (result.Details.Count > 0)
                        {
                            using (var detailDialog = new Form { Text = fa ? "جزئیات مغایرت‌ها" : "Migration discrepancies",
                                Width = 950, Height = 550, StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Canvas })
                            {
                                var detailLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 2, ColumnCount = 1 };
                                detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                                detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                                var filter = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                                filter.Items.Add(fa ? "همه مغایرت‌ها" : "All discrepancies");
                                foreach (var name in result.Details.Select(x => x.Check).Distinct().OrderBy(x => x)) filter.Items.Add(name);
                                var detailsGrid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                                    AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                                detailsGrid.Columns.Add("Check", "Check"); detailsGrid.Columns.Add("Id", "Record ID");
                                detailsGrid.Columns.Add("Issue", "Issue"); detailsGrid.Columns.Add("Expected", "Expected");
                                detailsGrid.Columns.Add("Actual", "Actual");
                                filter.SelectedIndexChanged += (s2, e2) => {
                                    detailsGrid.Rows.Clear();
                                    foreach (var detail in result.Details.Where(x => filter.SelectedIndex == 0 || x.Check == filter.SelectedItem.ToString()))
                                        detailsGrid.Rows.Add(detail.Check, detail.RecordId, detail.Issue, detail.ExpectedValue, detail.ActualValue);
                                };
                                detailLayout.Controls.Add(filter, 0, 0); detailLayout.Controls.Add(detailsGrid, 0, 1);
                                detailDialog.Controls.Add(detailLayout); filter.SelectedIndex = 0;
                                detailDialog.ShowDialog(dialog);
                            }
                        }
                        notice.Text = result.Passed ? (fa ? "شناسه‌ها، ارتباطات و محتوای رکوردها مطابقت دارند." : "IDs, relationships and record values match.") :
                            (fa ? "مغایرت یافت شد؛ قبل از تغییر دیتابیس گزارش را بررسی کنید." : "Differences found; review report before cutover.");
                        using (var save = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "AshkanLMS-Migration-Verification.csv" })
                            if (save.ShowDialog(dialog) == DialogResult.OK)
                                File.WriteAllText(save.FileName, result.ToCsv(), new UTF8Encoding(true));
                    } catch (Exception ex) {
                        notice.Text = fa ? "بررسی مقصد ناموفق: " + ex.Message : "Target verification failed: " + ex.Message;
                        MessageBox.Show(dialog, notice.Text, "SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                dialog.ShowDialog(this);
            }
        }
    }
}
