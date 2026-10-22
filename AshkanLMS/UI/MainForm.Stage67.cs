using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private sealed class DiagnosticBucket
        {
            public DateTime Day;
            public int Ui;
            public int Background;
            public int Total { get { return Ui + Background; } }
        }

        private void ShowSupportAnalytics()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            using (var dialog = new Form
            {
                Text = fa ? "تحلیل گزارش‌های تشخیصی" : "Diagnostic Analytics",
                Size = new Size(960, 700), MinimumSize = new Size(720, 520),
                StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Canvas,
                RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No
            })
            {
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 5 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                dialog.Controls.Add(root);
                root.Controls.Add(new Label { Dock = DockStyle.Fill, Text = fa ? "روند روزانه خطاهای ثبت‌شده" : "Daily trend of recorded events",
                    Font = Theme.Font(17, FontStyle.Bold), ForeColor = Theme.Text,
                    TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft }, 0, 0);
                var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
                    FlowDirection = fa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var range = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
                range.Items.AddRange(new object[] { fa ? "۷ روز" : "Last 7 days", fa ? "۳۰ روز" : "Last 30 days", fa ? "تمام گزارش‌ها" : "All available" });
                range.SelectedIndex = 0;
                var refresh = Theme.Button(fa ? "بازخوانی" : "Refresh"); refresh.Width = 120; refresh.Height = 34;
                var export = Theme.Button(fa ? "خروجی CSV" : "Export CSV"); export.Width = 130; export.Height = 34;
                toolbar.Controls.Add(range); toolbar.Controls.Add(refresh); toolbar.Controls.Add(export);
                root.Controls.Add(toolbar, 0, 1);
                var chart = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Card, Padding = new Padding(10) };
                root.Controls.Add(chart, 0, 2);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Theme.Card,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No };
                grid.Columns.Add("Day", fa ? "تاریخ (UTC)" : "Date (UTC)");
                grid.Columns.Add("UI", fa ? "رابط کاربری" : "UI events");
                grid.Columns.Add("Background", fa ? "پس‌زمینه" : "Background events");
                grid.Columns.Add("Total", fa ? "مجموع" : "Total");
                root.Controls.Add(grid, 0, 3);
                root.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = Theme.Font(9),
                    TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                    Text = fa ? "فقط رویدادهای محلی قابل‌خواندن؛ این گزارش تعداد کل خطاهای برنامه یا وضعیت SQL Server را تضمین نمی‌کند." :
                    "Readable local events only; this report does not represent all application failures or verify SQL Server." }, 0, 4);
                List<DiagnosticBucket> current = new List<DiagnosticBucket>();
                Action load = () =>
                {
                    var buckets = new SortedDictionary<DateTime, DiagnosticBucket>();
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS", "Diagnostics");
                    DateTime now = DateTime.UtcNow;
                    int days = range.SelectedIndex == 0 ? 7 : range.SelectedIndex == 1 ? 30 : -1;
                    foreach (string name in new[] { "events.previous.log", "events.log" })
                    {
                        string path = Path.Combine(folder, name);
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
                                if (days > 0 && (timestamp < now.AddDays(-days) || timestamp > now.AddMinutes(5))) continue;
                                DateTime day = timestamp.Date;
                                DiagnosticBucket bucket;
                                if (!buckets.TryGetValue(day, out bucket)) { bucket = new DiagnosticBucket { Day = day }; buckets.Add(day, bucket); }
                                if (parts[1] == "UI") bucket.Ui++; else bucket.Background++;
                            }
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                        catch (System.Security.SecurityException) { }
                    }
                    current = new List<DiagnosticBucket>(buckets.Values);
                    grid.Rows.Clear();
                    foreach (DiagnosticBucket b in current) grid.Rows.Add(b.Day.ToString("yyyy-MM-dd"), b.Ui, b.Background, b.Total);
                    chart.Invalidate();
                };
                chart.Paint += (sender, args) =>
                {
                    Graphics g = args.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int left = 44, top = 18, bottom = chart.Height - 28, right = chart.Width - 18;
                    if (right <= left || bottom <= top) return;
                    int max = 1; foreach (DiagnosticBucket b in current) if (b.Total > max) max = b.Total;
                    using (var axis = new Pen(Theme.Muted)) { g.DrawLine(axis, left, top, left, bottom); g.DrawLine(axis, left, bottom, right, bottom); }
                    if (current.Count == 0) { TextRenderer.DrawText(g, fa ? "هنوز رویداد قابل‌نمایشی وجود ندارد" : "No readable events for this period", Theme.Font(11), new Rectangle(left + 8, top + 8, Math.Max(10, right - left - 12), 30), Theme.Muted); return; }
                    float slot = (float)(right - left) / current.Count;
                    for (int i = 0; i < current.Count; i++)
                    {
                        DiagnosticBucket b = current[i]; int x = left + (int)(i * slot) + 3;
                        int width = Math.Max(3, (int)(slot * 0.68));
                        int hTotal = (int)((bottom - top - 18) * b.Total / (float)max);
                        int hUi = b.Total == 0 ? 0 : (int)(hTotal * b.Ui / (float)b.Total);
                        int hBg = hTotal - hUi;
                        if (hUi > 0) g.FillRectangle(new SolidBrush(Color.FromArgb(56, 120, 210)), x, bottom - hUi, width, hUi);
                        if (hBg > 0) g.FillRectangle(new SolidBrush(Color.FromArgb(235, 166, 60)), x, bottom - hTotal, width, hBg);
                        if (current.Count <= 10 || i % Math.Max(1, current.Count / 8) == 0)
                            TextRenderer.DrawText(g, b.Day.ToString("MM-dd"), Theme.Font(8), new Rectangle(x - 6, bottom + 3, Math.Max(width + 14, 35), 20), Theme.Muted);
                    }
                    TextRenderer.DrawText(g, fa ? "آبی: UI   |   کهربایی: Background" : "Blue: UI   |   Amber: Background", Theme.Font(9), new Point(left + 6, top), Theme.Text);
                };
                export.Click += (sender, args) =>
                {
                    using (var save = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "AshkanLMS-diagnostic-analytics.csv", OverwritePrompt = true })
                    {
                        if (save.ShowDialog(dialog) != DialogResult.OK) return;
                        var lines = new List<string> { "DateUtc,UiEvents,BackgroundEvents,Total" };
                        foreach (DiagnosticBucket b in current) lines.Add(b.Day.ToString("yyyy-MM-dd") + "," + b.Ui + "," + b.Background + "," + b.Total);
                        try { File.WriteAllLines(save.FileName, lines.ToArray(), new UTF8Encoding(true)); MessageBox.Show(dialog, fa ? "گزارش CSV ذخیره شد." : "CSV report saved.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Information); }
                        catch (IOException) { MessageBox.Show(dialog, fa ? "ذخیره فایل ممکن نشد." : "Could not save the file.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                        catch (UnauthorizedAccessException) { MessageBox.Show(dialog, fa ? "مجوز ذخیره فایل وجود ندارد." : "No permission to save the file.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                    }
                };
                refresh.Click += (sender, args) => load(); range.SelectedIndexChanged += (sender, args) => load();
                load(); dialog.ShowDialog(this);
            }
        }
    }
}
