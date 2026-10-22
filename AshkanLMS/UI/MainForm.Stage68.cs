using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
        private sealed class SafeDiagnosticEvent
        {
            public DateTime TimestampUtc { get; set; }
            public string Category { get; set; }
            public string Type { get; set; }
        }

        private void ShowSupportDiagnosticsCenterStage68()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            using (var dialog = new Form
            {
                Text = fa ? "مرکز پشتیبانی و عیب‌یابی" : "Support & Diagnostics Center",
                Size = new Size(980, 650), MinimumSize = new Size(760, 500),
                StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Canvas,
                RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No
            })
            {
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
                dialog.Controls.Add(root);
                root.Controls.Add(new Label { Dock = DockStyle.Fill, Text = fa ? "رویدادهای تشخیصی امن" : "Safe diagnostic events",
                    Font = Theme.Font(17, FontStyle.Bold), ForeColor = Theme.Text,
                    TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft }, 0, 0);

                var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
                    FlowDirection = fa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, WrapContents = false };
                var search = new TextBox { Width = 220, Height = 30, Font = Theme.Font(10) };
                search.AccessibleName = fa ? "جست‌وجوی نوع خطا" : "Search error type";
                var category = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
                category.Items.AddRange(new object[] { fa ? "همه دسته‌ها" : "All categories", "UI", "Background" }); category.SelectedIndex = 0;
                var period = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
                period.Items.AddRange(new object[] { fa ? "۲۴ ساعت اخیر" : "Last 24 hours", fa ? "۷ روز اخیر" : "Last 7 days", fa ? "۳۰ روز اخیر" : "Last 30 days", fa ? "همه زمان‌ها" : "All time" }); period.SelectedIndex = 1;
                var refresh = Theme.Button(fa ? "بازخوانی" : "Refresh"); refresh.Width = 105; refresh.Height = 34;
                var export = Theme.Button(fa ? "خروجی امن TXT" : "Export safe TXT"); export.Width = 145; export.Height = 34;
                var summary = Theme.Button(fa ? "گزارش آماری" : "Summary report"); summary.Width = 130; summary.Height = 34;
                filters.Controls.Add(search); filters.Controls.Add(category); filters.Controls.Add(period); filters.Controls.Add(refresh); filters.Controls.Add(export); filters.Controls.Add(summary);
                summary.Click += (s, e) => ShowDiagnosticSummaryStage69();
                root.Controls.Add(filters, 0, 1);

                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Theme.Card,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false, RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No };
                grid.Columns.Add("Timestamp", fa ? "زمان UTC" : "UTC time");
                grid.Columns.Add("Category", fa ? "دسته" : "Category");
                grid.Columns.Add("Type", fa ? "نوع خطا" : "Error type");
                root.Controls.Add(grid, 0, 2);
                var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
                var note = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = Theme.Font(9),
                    TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                    Text = fa ? "فقط مدیر سیستم. جزئیات شامل نوع خطا و فراداده‌های مجاز است؛ پیام استثنا، مسیر فایل و اطلاعات اتصال خوانده یا صادر نمی‌شوند." :
                    "Administrator only. Details are limited to safe metadata; exception messages, file paths and connection settings are never read or exported." };
                var count = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Text, Font = Theme.Font(10, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter };
                footer.Controls.Add(note, 0, 0); footer.Controls.Add(count, 1, 0); root.Controls.Add(footer, 0, 3);

                List<SafeDiagnosticEvent> allEvents = new List<SafeDiagnosticEvent>();
                Action readEvents = () =>
                {
                    allEvents.Clear();
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS", "Diagnostics");
                    foreach (string file in new[] { "events.previous.log", "events.log" })
                    {
                        string path = Path.Combine(folder, file);
                        try
                        {
                            if (!File.Exists(path) || new FileInfo(path).Length > 1048576) continue;
                            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                            {
                                string[] parts = line.Split(new[] { " | " }, StringSplitOptions.None);
                                DateTime timestamp;
                                if (parts.Length != 3 || (parts[1] != "UI" && parts[1] != "Background") ||
                                    !DateTime.TryParseExact(parts[0], "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture,
                                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp)) continue;
                                string type = parts[2];
                                if (type.Length < 1 || type.Length > 90 || type.Any(ch => !((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_'))) continue;
                                allEvents.Add(new SafeDiagnosticEvent { TimestampUtc = timestamp, Category = parts[1], Type = type });
                            }
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                        catch (System.Security.SecurityException) { }
                    }
                    allEvents = allEvents.OrderByDescending(e => e.TimestampUtc).Take(1000).ToList();
                };
                Action applyFilters = () =>
                {
                    DateTime cutoff = DateTime.MinValue;
                    if (period.SelectedIndex == 0) cutoff = DateTime.UtcNow.AddHours(-24);
                    else if (period.SelectedIndex == 1) cutoff = DateTime.UtcNow.AddDays(-7);
                    else if (period.SelectedIndex == 2) cutoff = DateTime.UtcNow.AddDays(-30);
                    string term = (search.Text ?? string.Empty).Trim();
                    IEnumerable<SafeDiagnosticEvent> filtered = allEvents;
                    if (cutoff != DateTime.MinValue) filtered = filtered.Where(e => e.TimestampUtc >= cutoff);
                    if (category.SelectedIndex == 1) filtered = filtered.Where(e => e.Category == "UI");
                    else if (category.SelectedIndex == 2) filtered = filtered.Where(e => e.Category == "Background");
                    if (term.Length > 0) filtered = filtered.Where(e => e.Type.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
                    var rows = filtered.Take(200).ToList();
                    grid.Rows.Clear();
                    foreach (SafeDiagnosticEvent e in rows) grid.Rows.Add(e.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC", e.Category, e.Type);
                    count.Text = fa ? "نمایش " + rows.Count + " رویداد" : "Showing " + rows.Count + " events";
                };
                refresh.Click += (s, e) => { readEvents(); applyFilters(); };
                search.TextChanged += (s, e) => applyFilters();
                category.SelectedIndexChanged += (s, e) => applyFilters();
                period.SelectedIndexChanged += (s, e) => applyFilters();
                grid.CellDoubleClick += (s, e) =>
                {
                    if (e.RowIndex < 0) return;
                    DataGridViewRow row = grid.Rows[e.RowIndex];
                    string details = Convert.ToString(row.Cells[0].Value) + Environment.NewLine +
                        (fa ? "دسته: " : "Category: ") + Convert.ToString(row.Cells[1].Value) + Environment.NewLine +
                        (fa ? "نوع خطا: " : "Error type: ") + Convert.ToString(row.Cells[2].Value) + Environment.NewLine + Environment.NewLine +
                        (fa ? "برای حفظ حریم خصوصی، متن استثنا و مسیر فایل نمایش داده نمی‌شوند." : "Exception text and file paths are intentionally not displayed for privacy.");
                    MessageBox.Show(dialog, details, fa ? "جزئیات امن رویداد" : "Safe event details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                export.Click += (s, e) =>
                {
                    using (var save = new SaveFileDialog { Filter = "Text report (*.txt)|*.txt", FileName = "AshkanLMS-safe-diagnostics.txt", OverwritePrompt = true })
                    {
                        if (save.ShowDialog(dialog) != DialogResult.OK) return;
                        var lines = new List<string> { "AshkanLMS sanitized diagnostics", "UTC | Category | ErrorType" };
                        foreach (DataGridViewRow row in grid.Rows)
                            if (!row.IsNewRow) lines.Add(Convert.ToString(row.Cells[0].Value) + " | " + Convert.ToString(row.Cells[1].Value) + " | " + Convert.ToString(row.Cells[2].Value));
                        try { File.WriteAllLines(save.FileName, lines, new UTF8Encoding(false)); MessageBox.Show(dialog, fa ? "گزارش امن ذخیره شد." : "Safe report saved.", dialog.Text); }
                        catch (IOException) { MessageBox.Show(dialog, fa ? "ذخیره فایل ممکن نشد." : "Could not save the report.", dialog.Text); }
                        catch (UnauthorizedAccessException) { MessageBox.Show(dialog, fa ? "مجوز ذخیره فایل وجود ندارد." : "No permission to save the report.", dialog.Text); }
                    }
                };
                readEvents(); applyFilters(); dialog.ShowDialog(this);
            }
        }
    }
}
