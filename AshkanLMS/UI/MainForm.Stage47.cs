using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private void ShowAuditReviewHistory(RelationalCoreRepository repository, bool fa)
        {
            AuthorizationService.RequireAccountAdmin();
            using (var dialog = new Form { Text = fa ? "تاریخچه رسیدگی امنیتی" : "Security review history",
                Size = new Size(1100, 690), MinimumSize = new Size(800, 480), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(243, 247, 252) })
            {
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 3 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                dialog.Controls.Add(root);
                var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
                root.Controls.Add(filters, 0, 0);
                var id = new TextBox { Width = 95 };
                var reviewer = new TextBox { Width = 155 };
                var status = new ComboBox { Width = 145, DropDownStyle = ComboBoxStyle.DropDownList };
                status.Items.AddRange(new object[] { fa ? "همه وضعیت‌ها" : "All statuses", "Open", "Investigating", "Resolved", "FalsePositive" });
                status.SelectedIndex = 0;
                var from = new DateTimePicker { Width = 125, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), ShowCheckBox = true, Checked = false };
                var until = new DateTimePicker { Width = 125, Format = DateTimePickerFormat.Short, Value = DateTime.Today, ShowCheckBox = true, Checked = false };
                filters.Controls.Add(new Label { Text = fa ? "شناسه رویداد" : "Audit ID", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
                filters.Controls.Add(id);
                filters.Controls.Add(new Label { Text = fa ? "بررسی‌کننده" : "Reviewer", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
                filters.Controls.Add(reviewer);
                filters.Controls.Add(status);
                filters.Controls.Add(new Label { Text = fa ? "از (UTC)" : "From (UTC)", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
                filters.Controls.Add(from);
                filters.Controls.Add(new Label { Text = fa ? "تا (UTC)" : "Through (UTC)", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
                filters.Controls.Add(until);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, BackgroundColor = Color.White };
                root.Controls.Add(grid, 0, 1);
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
                root.Controls.Add(actions, 0, 2);
                var search = new Button { Text = fa ? "جست‌وجو" : "Search", Width = 110, Height = 34 };
                var export = new Button { Text = fa ? "خروجی Excel" : "Export Excel", Width = 135, Height = 34, Enabled = false };
                var details = new Button { Text = fa ? "جزئیات" : "Details", Width = 110, Height = 34 };
                var count = new Label { AutoSize = true, Padding = new Padding(10, 8, 0, 0) };
                actions.Controls.Add(search); actions.Controls.Add(export); actions.Controls.Add(details); actions.Controls.Add(count);
                DataTable current = null;
                Action load = () =>
                {
                    long parsedId;
                    long? auditId = null;
                    if (!string.IsNullOrWhiteSpace(id.Text))
                    {
                        if (!long.TryParse(id.Text.Trim(), out parsedId) || parsedId <= 0)
                            throw new ArgumentException(fa ? "شناسه رویداد نامعتبر است." : "Invalid audit ID.");
                        auditId = parsedId;
                    }
                    DateTime? start = from.Checked ? (DateTime?)from.Value.Date : null;
                    DateTime? end = until.Checked ? (DateTime?)until.Value.Date.AddDays(1) : null;
                    current = repository.SearchAuditReviewHistory(auditId, reviewer.Text,
                        status.SelectedIndex == 0 ? null : Convert.ToString(status.SelectedItem), start, end);
                    grid.DataSource = current;
                    count.Text = (fa ? "تعداد (حداکثر ۵۰۰): " : "Rows (max 500): ") + current.Rows.Count;
                    export.Enabled = true;
                };
                Action showDetails = () =>
                {
                    var selected = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
                    if (selected == null) return;
                    using (var popup = new Form { Text = fa ? "جزئیات تغییر" : "Review change details", Size = new Size(700, 490), StartPosition = FormStartPosition.CenterParent })
                    {
                        var text = new System.Text.StringBuilder();
                        foreach (DataColumn column in selected.Row.Table.Columns)
                            text.AppendLine(column.ColumnName + ": " + Convert.ToString(selected[column.ColumnName]));
                        popup.Controls.Add(new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true,
                            ScrollBars = ScrollBars.Both, Text = text.ToString() });
                        popup.ShowDialog(dialog);
                    }
                };
                search.Click += (sender, args) => { try { load(); } catch (Exception ex) { MessageBox.Show(dialog, ex.Message); } };
                details.Click += (sender, args) => showDetails();
                grid.CellDoubleClick += (sender, args) => { if (args.RowIndex >= 0) showDetails(); };
                Action invalidate = () => { export.Enabled = false; };
                id.TextChanged += (sender, args) => invalidate();
                reviewer.TextChanged += (sender, args) => invalidate();
                status.SelectedIndexChanged += (sender, args) => invalidate();
                from.ValueChanged += (sender, args) => invalidate();
                until.ValueChanged += (sender, args) => invalidate();
                export.Click += (sender, args) =>
                {
                    if (current == null || !export.Enabled) return;
                    using (var save = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = "AshkanLMS-ReviewHistory.xlsx" })
                    {
                        if (save.ShowDialog(dialog) != DialogResult.OK) return;
                        try
                        {
                            var dataset = new DataSet();
                            dataset.Tables.Add(current.Copy());
                            AuditExcelExporter.Export(save.FileName, dataset, new[] { "Review History" });
                            MessageBox.Show(dialog, fa ? "گزارش ذخیره شد." : "Report saved.");
                        }
                        catch (Exception ex) { MessageBox.Show(dialog, ex.Message); }
                    }
                };
                try { load(); dialog.ShowDialog(this); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Review history", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
