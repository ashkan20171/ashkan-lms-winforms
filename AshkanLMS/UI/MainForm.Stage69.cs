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
        private sealed class Stage69SummaryRow
        {
            public string Category { get; set; }
            public string Type { get; set; }
            public int Count { get; set; }
            public DateTime FirstUtc { get; set; }
            public DateTime LastUtc { get; set; }
        }

        private void ShowDiagnosticSummaryStage69()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            var events = new List<Tuple<DateTime, string, string>>();
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS", "Diagnostics");
            foreach (string name in new[] { "events.previous.log", "events.log" })
            {
                string path = Path.Combine(folder, name);
                try
                {
                    if (!File.Exists(path) || new FileInfo(path).Length > 1048576) continue;
                    foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                    {
                        string[] parts = line.Split(new[] { " | " }, StringSplitOptions.None);
                        DateTime utc;
                        if (parts.Length != 3 || (parts[1] != "UI" && parts[1] != "Background") ||
                            !DateTime.TryParseExact(parts[0], "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc)) continue;
                        string type = parts[2];
                        if (type.Length < 1 || type.Length > 90 || type.Any(ch => !((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_'))) continue;
                        events.Add(Tuple.Create(utc, parts[1], type));
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }
            }
            var summaryRows = events.GroupBy(x => new { x.Item2, x.Item3 })
                .Select(g => new Stage69SummaryRow { Category = g.Key.Item2, Type = g.Key.Item3, Count = g.Count(),
                    FirstUtc = g.Min(x => x.Item1), LastUtc = g.Max(x => x.Item1) })
                .OrderByDescending(x => x.Count).ThenBy(x => x.Category).ThenBy(x => x.Type).ToList();

            using (var dialog = new Form { Text = fa ? "گزارش آماری خطاها" : "Diagnostic Summary Report",
                Size = new Size(900, 580), MinimumSize = new Size(720, 430), StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.Canvas, RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No })
            {
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
                dialog.Controls.Add(root);
                int total = events.Count, repeated = events.GroupBy(x => new { x.Item2, x.Item3 }).Sum(g => Math.Max(0, g.Count() - 1));
                int types = summaryRows.Count;
                var stats = new Label { Dock = DockStyle.Fill, Font = Theme.Font(11, FontStyle.Bold), ForeColor = Theme.Text,
                    TextAlign = fa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                    Text = fa ? string.Format("کل رویدادها: {0}    انواع دسته/خطا: {1}    تکرارهای اضافی: {2}", total, types, repeated) :
                        string.Format("Total events: {0}    Category/type groups: {1}    Repeated occurrences: {2}", total, types, repeated) };
                root.Controls.Add(stats, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                    RowHeadersVisible = false, BackgroundColor = Theme.Card, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No,
                    AllowUserToOrderColumns = true, AutoGenerateColumns = false };
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = fa ? "دسته" : "Category", DataPropertyName = "Category", SortMode = DataGridViewColumnSortMode.Automatic });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = fa ? "نوع خطا" : "Error type", DataPropertyName = "Type", SortMode = DataGridViewColumnSortMode.Automatic });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = fa ? "تعداد" : "Count", DataPropertyName = "Count", SortMode = DataGridViewColumnSortMode.Automatic });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FirstUtc", HeaderText = fa ? "اولین رخداد UTC" : "First occurrence UTC", DataPropertyName = "FirstUtc", SortMode = DataGridViewColumnSortMode.Automatic });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastUtc", HeaderText = fa ? "آخرین رخداد UTC" : "Last occurrence UTC", DataPropertyName = "LastUtc", SortMode = DataGridViewColumnSortMode.Automatic });
                grid.DataSource = summaryRows.Select(x => new { x.Category, x.Type, x.Count,
                    FirstUtc = x.FirstUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    LastUtc = x.LastUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }).ToList();
                root.Controls.Add(grid, 0, 1);
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = fa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var export = Theme.Button(fa ? "خروجی CSV" : "Export CSV"); export.Width = 130; export.Height = 34;
                var close = Theme.Button(fa ? "بستن" : "Close"); close.Width = 100; close.Height = 34; close.Click += (s, e) => dialog.Close();
                actions.Controls.Add(export); actions.Controls.Add(close); root.Controls.Add(actions, 0, 2);
                export.Click += (s, e) =>
                {
                    using (var save = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "AshkanLMS-diagnostic-summary.csv", OverwritePrompt = true })
                    {
                        if (save.ShowDialog(dialog) != DialogResult.OK) return;
                        Func<string, string> csv = value => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
                        var lines = new List<string> { "Category,ErrorType,Count,FirstUtc,LastUtc" };
                        foreach (var item in summaryRows) lines.Add(string.Join(",", csv(item.Category), csv(item.Type), item.Count.ToString(CultureInfo.InvariantCulture),
                            csv(item.FirstUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)), csv(item.LastUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))));
                        try { File.WriteAllLines(save.FileName, lines, new UTF8Encoding(true)); MessageBox.Show(dialog, fa ? "گزارش آماری ذخیره شد." : "Summary report saved.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Information); }
                        catch (IOException) { MessageBox.Show(dialog, fa ? "ذخیره فایل ممکن نشد." : "Could not save the report.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                        catch (UnauthorizedAccessException) { MessageBox.Show(dialog, fa ? "مجوز ذخیره فایل وجود ندارد." : "No permission to save the report.", dialog.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                    }
                };
                dialog.ShowDialog(this);
            }
        }
    }
}
