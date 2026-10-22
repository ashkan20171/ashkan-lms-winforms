using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private void ShowAuditReviewSignals(DataTable signals, bool persian)
        {
            AuthorizationService.RequireAccountAdmin();
            using (var window = new Form())
            {
                window.Text = persian ? "بررسی حذف اطلاعات" : "Deletion review";
                window.Size = new Size(940, 580);
                window.MinimumSize = new Size(660, 400);
                window.StartPosition = FormStartPosition.CenterParent;
                window.BackColor = Color.FromArgb(244, 247, 251);
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                window.Controls.Add(root);
                var title = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 47, 76), TextAlign = ContentAlignment.MiddleLeft,
                    Text = (persian ? "حذف‌های نیازمند بررسی: " : "Deletions to review: ") + signals.Rows.Count +
                        (persian ? "  |  حداکثر ۳۰۰ رویداد اخیر در بازه انتخابی" : "  |  Latest 300 events in selected period") };
                root.Controls.Add(title, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowHeadersVisible = false, BackgroundColor = Color.White, DataSource = signals };
                root.Controls.Add(grid, 0, 1);
                foreach (var column in new[] { "OldValues", "NewValues" })
                    if (grid.Columns.Contains(column)) grid.Columns[column].Visible = false;
                grid.CellDoubleClick += (sender, args) =>
                {
                    if (args.RowIndex < 0 || args.RowIndex >= grid.Rows.Count) return;
                    var view = grid.Rows[args.RowIndex].DataBoundItem as DataRowView;
                    if (view == null) return;
                    string details = (persian ? "شناسه رویداد: " : "Audit ID: ") + view["AuditId"] +
                        "\n" + (persian ? "مقادیر قبل از حذف:" : "Values before deletion:") +
                        "\n" + Convert.ToString(view["OldValues"]);
                    using (var detailsWindow = new Form { Text = persian ? "جزئیات رویداد" : "Event details",
                        Size = new Size(650, 440), StartPosition = FormStartPosition.CenterParent })
                    {
                        detailsWindow.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true,
                            ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = true, Text = details });
                        detailsWindow.ShowDialog(window);
                    }
                };
                window.ShowDialog(this);
            }
        }
    }
}
