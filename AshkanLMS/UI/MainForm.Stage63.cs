using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Available from the main navigation in both JSON and SQL modes.
        private void ShowSupportDiagnosticsPage()
        {
            AuthorizationService.RequireAccountAdmin();
            Clear();
            bool fa = AppSession.Persian;
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28),
                BackColor = Theme.Canvas, ColumnCount = 1, RowCount = 4 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.Controls.Add(panel);
            var heading = new Label { Text = fa ? "مرکز پشتیبانی و عیب‌یابی" : "Support & Diagnostics Center",
                Dock = DockStyle.Fill, Font = Theme.Font(19, FontStyle.Bold), ForeColor = Theme.Text,
                TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };
            panel.Controls.Add(heading, 0, 0);
            var description = new Label { Text = fa ?
                "گزارش خطاهای محلی را بدون نیاز به اتصال SQL Server بررسی کنید. این صفحه فقط برای مدیر سیستم در دسترس است و هیچ داده‌ای ارسال نمی‌کند." :
                "Inspect local diagnostic events without requiring SQL Server. This administrator-only page does not transmit data.",
                Dock = DockStyle.Fill, Font = Theme.Font(11), ForeColor = Theme.Muted,
                TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };
            panel.Controls.Add(description, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
                FlowDirection = fa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            var open = Theme.Button(fa ? "مشاهده گزارش‌ها" : "View diagnostic events");
            open.Width = 200; open.Height = 42;
            open.Click += (sender, args) => ShowSupportDiagnosticsCenterStage68();
            var health = Theme.Button(fa ? "سلامت برنامه" : "Health overview");
            health.Width = 190; health.Height = 42;
            health.Click += (sender, args) => ShowSupportHealthDashboard();
            var help = Theme.Button(fa ? "راهنمای رفع خطا" : "Troubleshooting guide");
            help.Width = 200; help.Height = 42;
            help.Click += (sender, args) => ShowSupportTroubleshootingGuide();
            var analytics = Theme.Button(fa ? "تحلیل گزارش‌ها" : "Diagnostic analytics");
            analytics.Width = 190; analytics.Height = 42;
            analytics.Click += (sender, args) => ShowSupportAnalytics();
            actions.Controls.Add(health); actions.Controls.Add(open); actions.Controls.Add(help); actions.Controls.Add(analytics);
            panel.Controls.Add(actions, 0, 2);
            var tip = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Text,
                Font = Theme.Font(10), TextAlign = fa ? ContentAlignment.TopRight : ContentAlignment.TopLeft,
                Text = fa ? "نکته: فایل‌های خطا در پوشه Diagnostics داخل AppData محلی ذخیره می‌شوند. گزارش امن شامل پیام خطا یا اطلاعات اتصال نیست." :
                "Tip: Diagnostic events are stored in the local AppData Diagnostics folder. Safe exports exclude exception messages and connection details." };
            panel.Controls.Add(tip, 0, 3);
        }

        private void ShowSupportTroubleshootingGuide()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            string message = fa ?
                "برنامه اجرا نمی‌شود: نصب .NET Framework 4.8 و وجود فایل‌های Release را بررسی کنید.\n\n" +
                "مشکل ورود: نام کاربری، وضعیت حساب و مجوزهای پوشه داده محلی را بررسی کنید.\n\n" +
                "خطای دیتابیس: حالت Json به SQL Server نیاز ندارد؛ برای محیط SQL، سرویس، اتصال و اسکریپت‌های دیتابیس را جداگانه بررسی کنید.\n\n" +
                "خطای دسترسی: برای مشاهده گزارش‌ها باید با حساب مدیر وارد شوید.\n\n" +
                "برای تشخیص بیشتر، ابزار tools\\diagnose-install.ps1 را روی ویندوز اجرا کنید. هیچ تنظیم یا دیتابیسی در این صفحه تغییر نمی‌کند." :
                "App will not start: check .NET Framework 4.8 and Release files.\n\n" +
                "Sign-in fails: check account status and local data folder permissions.\n\n" +
                "Database errors: JSON mode does not require SQL Server. For SQL workspace, check the service, connection and schema separately.\n\n" +
                "Access denied: sign in as an administrator to view events.\n\n" +
                "For further diagnostics run tools\\diagnose-install.ps1 on Windows. This guide does not modify configuration or databases.";
            MessageBox.Show(this, message, fa ? "راهنمای رفع خطا" : "Troubleshooting guide",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowSupportDiagnosticsCenter()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            using (var dialog = new Form { Text = fa ? "مرکز پشتیبانی و عیب‌یابی" : "Support & Diagnostics Center",
                Size = new Size(850, 560), MinimumSize = new Size(620, 400), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(243, 247, 252), RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 3 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                dialog.Controls.Add(layout);
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
                var refresh = new Button { Text = fa ? "بازخوانی" : "Refresh", Width = 125, Height = 36 };
                var export = new Button { Text = fa ? "ذخیره گزارش امن" : "Export safe report", Width = 175, Height = 36 };
                buttons.Controls.Add(refresh);
                buttons.Controls.Add(export);
                layout.Controls.Add(buttons, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Color.White,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
                grid.Columns.Add("Timestamp", fa ? "زمان UTC" : "UTC time");
                grid.Columns.Add("Category", fa ? "دسته" : "Category");
                grid.Columns.Add("Type", fa ? "نوع خطا" : "Error type");
                layout.Controls.Add(grid, 0, 1);
                var note = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(55, 75, 105),
                    Text = fa ? "فقط مدیر سیستم؛ نمایش حداکثر ۲۰۰ رویداد اخیر. گزارش خروجی شامل نام کاربر، مسیر فایل، پیام خطا یا اطلاعات اتصال نیست. در صورت نبود گزارش، ثبت خطایی انجام نشده است." :
                    "Administrator only. Shows up to 200 recent events. Exports omit user names, paths, exception messages and connection settings. No events means no recorded errors.",
                    Padding = new Padding(6, 10, 6, 0) };
                layout.Controls.Add(note, 0, 2);
                Action load = () =>
                {
                    grid.Rows.Clear();
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS", "Diagnostics");
                    var events = new List<string[]>();
                    foreach (string file in new[] { "events.previous.log", "events.log" })
                    {
                        string path = Path.Combine(folder, file);
                        try
                        {
                            if (!File.Exists(path)) continue;
                            // Limit file size before reading, including for unexpectedly modified files.
                            if (new FileInfo(path).Length > 1048576) continue;
                            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                            {
                                string[] parts = line.Split(new[] { " | " }, StringSplitOptions.None);
                                if (parts.Length != 3) continue;
                                DateTime timestamp;
                                if (!DateTime.TryParseExact(parts[0], "yyyy-MM-ddTHH:mm:ssZ",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out timestamp)) continue;
                                if (parts[1] != "UI" && parts[1] != "Background") continue;
                                if (parts[2].Length < 1 || parts[2].Length > 90) continue;
                                bool safe = true;
                                foreach (char ch in parts[2]) if (!(char.IsLetterOrDigit(ch) && ch < 128) && ch != '_') { safe = false; break; }
                                if (!safe) continue;
                                events.Add(new[] { timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " UTC", parts[1], parts[2] });
                            }
                        }
                        catch (IOException) { /* File may be rotated while reading. */ }
                        catch (UnauthorizedAccessException) { }
                        catch (System.Security.SecurityException) { }
                    }
                    int start = Math.Max(0, events.Count - 200);
                    for (int i = events.Count - 1; i >= start; i--) grid.Rows.Add(events[i]);
                };
                refresh.Click += (sender, args) => load();
                export.Click += (sender, args) =>
                {
                    using (var save = new SaveFileDialog { Filter = "Text report (*.txt)|*.txt", FileName = "AshkanLMS-safe-diagnostics.txt",
                        OverwritePrompt = true })
                    {
                        if (save.ShowDialog(dialog) != DialogResult.OK) return;
                        var lines = new List<string> { "AshkanLMS sanitized diagnostics", "UTC | Category | Exception type" };
                        foreach (DataGridViewRow row in grid.Rows)
                            if (!row.IsNewRow) lines.Add(Convert.ToString(row.Cells[0].Value) + " | " +
                                Convert.ToString(row.Cells[1].Value) + " | " + Convert.ToString(row.Cells[2].Value));
                        try { File.WriteAllLines(save.FileName, lines.ToArray(), new UTF8Encoding(false));
                            MessageBox.Show(fa ? "گزارش امن ذخیره شد." : "Safe report saved.", dialog.Text); }
                        catch (IOException) { MessageBox.Show(fa ? "ذخیره فایل ممکن نشد." : "Could not save report.", dialog.Text); }
                        catch (UnauthorizedAccessException) { MessageBox.Show(fa ? "مجوز ذخیره فایل وجود ندارد." : "No permission to save report.", dialog.Text); }
                    }
                };
                load();
                dialog.ShowDialog(this);
            }
        }
    }
}
