using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private void ShowReviewStatusOverview(RelationalCoreRepository repository, DateTime fromUtc,
            DateTime untilUtcExclusive, string actor, bool fa)
        {
            AuthorizationService.RequireAccountAdmin();
            using (var dialog = new Form { Text = fa ? "خلاصه رسیدگی امنیتی" : "Security review overview",
                Size = new Size(760, 530), MinimumSize = new Size(580, 390),
                StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(243, 247, 252) })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                dialog.Controls.Add(layout);
                var title = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(35, 55, 84), TextAlign = ContentAlignment.MiddleLeft };
                layout.Controls.Add(title, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                    AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White };
                layout.Controls.Add(grid, 0, 1);
                var footer = new FlowLayoutPanel { Dock = DockStyle.Fill };
                layout.Controls.Add(footer, 0, 2);
                var refresh = new Button { Text = fa ? "بازخوانی" : "Refresh", Width = 115, Height = 34 };
                var close = new Button { Text = fa ? "بستن" : "Close", Width = 115, Height = 34 };
                footer.Controls.Add(refresh); footer.Controls.Add(close);
                var signals = new Button { Text = fa ? "قانون حجم حذف" : "Deletion threshold", Width = 160, Height = 34 };
                footer.Controls.Add(signals);
                signals.Click += (sender, args) =>
                {
                    try { ShowDeletionThresholdSignals(repository, fromUtc, untilUtcExclusive, actor, fa); }
                    catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Deletion signals", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                Action reload = () =>
                {
                    var result = repository.GetReviewStatusSummary(fromUtc, untilUtcExclusive, actor);
                    grid.DataSource = result;
                    long total = 0, unreviewed = 0;
                    foreach (DataRow row in result.Rows)
                    {
                        long count = Convert.ToInt64(row["EventCount"]);
                        total += count;
                        if (Convert.ToString(row["ReviewStatus"]) == "Unreviewed") unreviewed += count;
                    }
                    title.Text = (fa ? "رویدادهای حذف: " : "Deletion events: ") + total +
                        (fa ? "  |  بررسی‌نشده: " : "  |  Unreviewed: ") + unreviewed;
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (Convert.ToString(row.Cells["ReviewStatus"].Value) == "Unreviewed")
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 221);
                    }
                };
                refresh.Click += (sender, args) =>
                {
                    try { reload(); }
                    catch (Exception ex) { MessageBox.Show(dialog, ex.Message); }
                };
                close.Click += (sender, args) => dialog.Close();
                reload();
                dialog.ShowDialog(this);
            }
        }
    }
}
