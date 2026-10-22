using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private void ShowAuditDashboard(RelationalCoreRepository repository, bool persian)
        {
            AuthorizationService.RequireAccountAdmin();
            using (var window = new Form())
            {
                window.Text = persian ? "تحلیل تاریخچه تغییرات" : "Audit analytics";
                window.Size = new Size(1020, 720);
                window.StartPosition = FormStartPosition.CenterParent;
                window.MinimumSize = new Size(750, 550);
                window.BackColor = Color.FromArgb(244, 247, 251);
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                window.Controls.Add(root);
                var header = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(30, 47, 76), TextAlign = ContentAlignment.MiddleLeft,
                    Text = persian ? "گزارش فعالیت‌ها (UTC)" : "Activity insights (UTC)" };
                root.Controls.Add(header, 0, 0);
                var controls = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
                root.Controls.Add(controls, 0, 1);
                controls.Controls.Add(new Label { Text = persian ? "از تاریخ UTC" : "From UTC", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
                var from = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 125, Value = DateTime.UtcNow.Date.AddDays(-29) };
                controls.Controls.Add(from);
                controls.Controls.Add(new Label { Text = persian ? "تا تاریخ UTC" : "Through UTC", AutoSize = true, Padding = new Padding(10, 8, 0, 0) });
                var through = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 125, Value = DateTime.UtcNow.Date };
                controls.Controls.Add(through);
                var refresh = new Button { Text = persian ? "تحلیل" : "Analyze", Width = 95, Height = 29 };
                controls.Controls.Add(refresh);
                controls.Controls.Add(new Label { Text = persian ? "کاربر" : "Actor", AutoSize = true, Padding = new Padding(10, 8, 0, 0) });
                var actorFilter = new TextBox { Width = 135 };
                controls.Controls.Add(actorFilter);
                var kpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.FromArgb(244, 247, 251) };
                for (int i = 0; i < 3; i++) kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
                var cards = new Label[3];
                for (int i = 0; i < 3; i++)
                {
                    cards[i] = new Label { Dock = DockStyle.Fill, Margin = new Padding(5), BackColor = Color.White,
                        ForeColor = Color.FromArgb(30, 47, 76), Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
                    kpis.Controls.Add(cards[i], i, 0);
                }
                root.Controls.Add(kpis, 0, 2);
                var tabs = new TabControl { Dock = DockStyle.Fill };
                root.Controls.Add(tabs, 0, 3);
                var titles = persian ? new[] { "روزانه", "نوع عملیات", "کاربران", "حجم بالا" } : new[] { "Daily", "Operations", "Actors", "High volume" };
                var grids = new DataGridView[4];
                var charts = new Chart[2];
                DataSet lastData = null;
                var export = new Button { Text = persian ? "خروجی Excel" : "Export Excel", Width = 115, Height = 29 };
                controls.Controls.Add(export);
                var review = new Button { Text = persian ? "بررسی هشدارها" : "Review signals", Width = 120, Height = 29 };
                controls.Controls.Add(review);
                for (int i = 0; i < grids.Length; i++)
                {
                    var page = new TabPage(titles[i]);
                    grids[i] = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        BackgroundColor = Color.White, RowHeadersVisible = false };
                    if (i < 2)
                    {
                        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
                        charts[i] = new Chart { Dock = DockStyle.Fill, BackColor = Color.White, Palette = ChartColorPalette.BrightPastel };
                        charts[i].ChartAreas.Add(new ChartArea("Main"));
                        charts[i].ChartAreas[0].AxisX.Interval = 1;
                        charts[i].ChartAreas[0].AxisX.LabelStyle.Angle = -35;
                        charts[i].ChartAreas[0].AxisY.Minimum = 0;
                        split.Panel1.Controls.Add(charts[i]);
                        split.Panel2.Controls.Add(grids[i]);
                        page.Controls.Add(split);
                    }
                    else page.Controls.Add(grids[i]);
                    tabs.TabPages.Add(page);
                }
                Action load = () =>
                {
                    try
                    {
                        lastData = null;
                        export.Enabled = false;
                        var start = from.Value.Date;
                        var end = through.Value.Date.AddDays(1);
                        if (start >= end || (end - start).TotalDays > 366)
                            throw new ArgumentException(persian ? "بازه باید حداکثر ۳۶۶ روز باشد." : "Range must be at most 366 days.");
                        var data = repository.GetAuditDashboardForActor(start, end, actorFilter.Text);
                        for (int i = 0; i < grids.Length; i++) grids[i].DataSource = data.Tables[i];
                        lastData = data;
                        export.Enabled = true;
                        DrawAuditChart(charts[0], data.Tables[0], SeriesChartType.Column, "Events");
                        DrawAuditChart(charts[1], data.Tables[1], SeriesChartType.Bar, "Events");
                        long total = 0;
                        foreach (DataRow row in data.Tables[0].Rows) total += Convert.ToInt64(row["Events"]);
                        cards[0].Text = (persian ? "کل رویدادها\n" : "Total events\n") + total.ToString("N0");
                        cards[1].Text = (persian ? "روزهای فعال\n" : "Active days\n") + data.Tables[0].Rows.Count;
                        cards[2].Text = (persian ? "کاربران فعال (۱۵ نفر برتر)\n" : "Top active actors\n") + data.Tables[2].Rows.Count;
                        header.Text = (persian ? "رویدادهای ثبت‌شده: " : "Recorded events: ") + total.ToString("N0") +
                            (persian ? " | روزهای دارای فعالیت: " : " | Active days: ") + data.Tables[0].Rows.Count;
                    }
                    catch (Exception ex) { MessageBox.Show(window, ex.Message, "Audit analytics", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };
                Action invalidate = () => { lastData = null; export.Enabled = false; };
                actorFilter.TextChanged += (sender, args) => invalidate();
                from.ValueChanged += (sender, args) => invalidate();
                through.ValueChanged += (sender, args) => invalidate();
                export.Enabled = false;
                export.Click += (sender, args) =>
                {
                    if (lastData == null) { MessageBox.Show(window, persian ? "ابتدا گزارش را بارگذاری کنید." : "Load the report first."); return; }
                    using (var dialog = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = "AshkanLMS-Audit-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".xlsx" })
                    {
                        if (dialog.ShowDialog(window) != DialogResult.OK) return;
                        try { AuditExcelExporter.Export(dialog.FileName, lastData, titles); MessageBox.Show(window, persian ? "فایل Excel ذخیره شد." : "Excel file saved."); }
                        catch (Exception ex) { MessageBox.Show(window, ex.Message, "Excel", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
                };
                review.Click += (sender, args) =>
                {
                    try
                    {
                        var start = from.Value.Date;
                        var end = through.Value.Date.AddDays(1);
                        if (start >= end || (end - start).TotalDays > 366)
                            throw new ArgumentException(persian ? "بازه زمانی نامعتبر است." : "Invalid date range.");
                        ShowAuditReviewCenter(repository, start, end, actorFilter.Text, persian);
                    }
                    catch (Exception ex) { MessageBox.Show(window, ex.Message, "Audit review", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };
                refresh.Click += (sender, args) => load();
                window.Shown += (sender, args) => load();
                window.ShowDialog(this);
            }
        }
        private static void DrawAuditChart(Chart chart, DataTable table, SeriesChartType type, string valueColumn)
        {
            chart.Series.Clear();
            if (table.Columns.Count < 2 || !table.Columns.Contains(valueColumn)) return;
            var series = new Series("Events") { ChartType = type, IsValueShownAsLabel = true, ChartArea = "Main" };
            foreach (DataRow row in table.Rows)
            {
                string label = Convert.ToString(row[0]);
                if (table.Columns.Count > 2 && table.Columns[0].ColumnName.IndexOf("Entity", StringComparison.OrdinalIgnoreCase) >= 0)
                    label += " / " + Convert.ToString(row[1]);
                if (label.Length > 28) label = label.Substring(0, 28) + "…";
                series.Points.AddXY(label, Convert.ToDouble(row[valueColumn]));
            }
            chart.Series.Add(series);
        }
    }
}
