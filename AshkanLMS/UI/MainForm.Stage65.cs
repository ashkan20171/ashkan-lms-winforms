using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Read-only, local checks. SQL connectivity and data integrity are NOT inferred here.
        private void ShowSupportHealthDashboard()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            using (var dialog = new Form {
                Text = fa ? "نمای کلی سلامت برنامه" : "Application Health Overview",
                Size = new Size(900, 620), MinimumSize = new Size(650, 450),
                StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Canvas,
                RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No
            })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
                dialog.Controls.Add(layout);
                layout.Controls.Add(new Label { Dock = DockStyle.Fill,
                    Text = fa ? "وضعیت سرویس‌های محلی" : "Local health checks",
                    Font = Theme.Font(17, FontStyle.Bold), ForeColor = Theme.Text,
                    TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft }, 0, 0);
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = fa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var refresh = Theme.Button(fa ? "بازخوانی وضعیت" : "Refresh checks");
                refresh.Width = 170; refresh.Height = 40;
                actions.Controls.Add(refresh);
                var filterLabel = new Label { AutoSize = true, Text = fa ? "بازه گزارش‌ها:" : "Event window:", ForeColor = Theme.Text, Padding = new Padding(6, 12, 4, 0) };
                var period = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Height = 36 };
                period.Items.AddRange(new object[] { fa ? "۲۴ ساعت اخیر" : "Last 24 hours", fa ? "۷ روز اخیر" : "Last 7 days", fa ? "۳۰ روز اخیر" : "Last 30 days", fa ? "تمام گزارش‌های موجود" : "All available" });
                period.SelectedIndex = 1;
                actions.Controls.Add(filterLabel); actions.Controls.Add(period);
                layout.Controls.Add(actions, 0, 1);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                    AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                    BackgroundColor = Theme.Card, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No };
                grid.Columns.Add("Check", fa ? "بررسی" : "Check");
                grid.Columns.Add("Status", fa ? "وضعیت" : "Status");
                grid.Columns.Add("Detail", fa ? "توضیح" : "Details");
                layout.Controls.Add(grid, 0, 2);
                var summary = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Text, Font = Theme.Font(11, FontStyle.Bold), TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };
                layout.Controls.Add(summary, 0, 3);
                layout.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted,
                    Font = Theme.Font(10), TextAlign = fa ? ContentAlignment.TopRight : ContentAlignment.TopLeft,
                    Text = fa ? "فقط بررسی محلی و بدون تغییر داده. وضعیت SQL Server، صحت دیتابیس، نصب و عملکرد برنامه در این صفحه آزمایش نمی‌شود. جزئیات اتصال یا مسیرهای شخصی نمایش داده نمی‌شوند." :
                    "Read-only local checks only. SQL connectivity, database integrity, installation and application functionality are not tested. No connection details or personal paths are shown." }, 0, 4);
                Action load = () => {
                    grid.Rows.Clear();
                    summary.Text = fa ? "در حال بررسی گزارش‌های محلی" : "Checking local event logs";
                    Action<string,string,string> add = (name,status,detail) => grid.Rows.Add(name,status,detail);
                    string ok = fa ? "موجود" : "Present";
                    string missing = fa ? "یافت نشد" : "Missing";
                    string unknown = fa ? "نامشخص" : "Unknown";
                    try {
                        string mode = ConfigurationManager.AppSettings["PersistenceMode"];
                        bool valid = string.Equals(mode, "Json", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(mode, "SqlServer", StringComparison.OrdinalIgnoreCase);
                        add(fa ? "حالت ذخیره‌سازی" : "Persistence mode", valid ? ok : unknown,
                            valid ? (string.Equals(mode, "Json", StringComparison.OrdinalIgnoreCase) ? "JSON" : "SQL Server (not connected)") :
                            (fa ? "مقدار تنظیمات نامعتبر یا خالی است" : "Invalid or empty setting"));
                    } catch (ConfigurationErrorsException) {
                        add(fa ? "تنظیمات" : "Configuration", unknown, fa ? "خواندن تنظیمات ممکن نشد" : "Cannot read configuration");
                    }
                    try {
                        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS");
                        add(fa ? "پوشه داده محلی" : "Local data folder", Directory.Exists(folder) ? ok : missing,
                            fa ? "بدون نمایش مسیر خصوصی" : "Path hidden");
                        string logs = Path.Combine(folder, "Diagnostics");
                        int count = 0;
                        int uiCount = 0;
                        int backgroundCount = 0;
                        DateTime latest = DateTime.MinValue;
                        foreach (string file in new[] { "events.previous.log", "events.log" }) {
                            string path = Path.Combine(logs, file);
                            if (!File.Exists(path)) continue;
                            var info = new FileInfo(path);
                            if (info.Length > 1048576) continue;
                            foreach (string line in File.ReadAllLines(path)) {
                                string[] parts = line.Split(new[] { " | " }, StringSplitOptions.None);
                                DateTime dt;
                                if (parts.Length != 3 || (parts[1] != "UI" && parts[1] != "Background") ||
                                    !DateTime.TryParseExact(parts[0], "yyyy-MM-ddTHH:mm:ssZ",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out dt)) continue;
                                int days = period.SelectedIndex == 0 ? 1 : period.SelectedIndex == 1 ? 7 : period.SelectedIndex == 2 ? 30 : -1;
                                if (days > 0 && (dt < DateTime.UtcNow.AddDays(-days) || dt > DateTime.UtcNow.AddMinutes(5))) continue;
                                count++;
                                if (parts[1] == "UI") uiCount++; else backgroundCount++;
                                if (dt > latest) latest = dt;
                            }
                        }
                        add(fa ? "رویدادهای ثبت‌شده" : "Recorded events", count.ToString(),
                            fa ? "تعداد رویدادهای قابل‌خواندن (نه تعداد کل خطاها)" : "Readable events only; not all failures");
                        add(fa ? "خطاهای رابط کاربری" : "UI events", uiCount.ToString(), fa ? "در بازه انتخاب‌شده" : "In selected window");
                        add(fa ? "خطاهای پس‌زمینه" : "Background events", backgroundCount.ToString(), fa ? "در بازه انتخاب‌شده" : "In selected window");
                        summary.Text = (fa ? "خلاصه: " : "Summary: ") + count + (fa ? " رویداد | UI: " : " events | UI: ") + uiCount + " | Background: " + backgroundCount;
                        add(fa ? "آخرین رویداد" : "Latest event", latest == DateTime.MinValue ? unknown : latest.ToString("yyyy-MM-dd HH:mm") + " UTC",
                            fa ? "زمان ثبت رویداد" : "Recorded UTC time");
                    } catch (IOException) { add(fa ? "گزارش‌ها" : "Diagnostic logs", unknown, fa ? "خطای خواندن فایل" : "File read failed"); }
                      catch (UnauthorizedAccessException) { add(fa ? "گزارش‌ها" : "Diagnostic logs", unknown, fa ? "دسترسی محدود است" : "Access denied"); }
                      catch (System.Security.SecurityException) { add(fa ? "گزارش‌ها" : "Diagnostic logs", unknown, fa ? "دسترسی محدود است" : "Access denied"); }
                    add(fa ? "اتصال SQL Server" : "SQL Server connection", fa ? "آزمایش نشده" : "Not tested",
                        fa ? "از Database Health Center برای آزمایش دستی استفاده کنید" : "Use Database Health Center for a separate check");
                };
                refresh.Click += (sender, args) => load();
                period.SelectedIndexChanged += (sender, args) => load();
                load();
                dialog.ShowDialog(this);
            }
        }
    }
}
