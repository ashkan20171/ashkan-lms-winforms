using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private void ShowDeletionThresholdSignals(RelationalCoreRepository repository, DateTime fromUtc,
            DateTime untilUtcExclusive, string actor, bool fa)
        {
            AuthorizationService.RequireAccountAdmin();
            using (var dialog = new Form { Text = fa ? "بررسی حجم حذف روزانه" : "Daily deletion threshold",
                Size = new Size(800, 530), MinimumSize = new Size(620, 390),
                StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(243, 247, 252) })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                dialog.Controls.Add(layout);
                var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
                top.Controls.Add(new Label { Text = fa ? "حداقل حذف در هر روز (UTC):" : "Daily deletions (UTC) at least:",
                    AutoSize = true, Padding = new Padding(0, 10, 0, 0) });
                var threshold = new NumericUpDown { Minimum = 1, Maximum = 10000, Value = 25, Width = 90 };
                top.Controls.Add(threshold);
                var saveRule = new Button { Text = fa ? "ذخیره قانون" : "Save rule", Width = 115, Height = 32 };
                top.Controls.Add(saveRule);
                var search = new Button { Text = fa ? "بررسی" : "Evaluate", Width = 110, Height = 32 };
                top.Controls.Add(search);
                layout.Controls.Add(top, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                    AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
                layout.Controls.Add(grid, 0, 1);
                var footer = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(75, 88, 105),
                    Text = fa ? "این نتایج فقط سیگنال آماری هستند و اثبات تخلف نیستند." :
                        "Statistical signals only; they do not establish wrongdoing." };
                layout.Controls.Add(footer, 0, 2);
                Action evaluate = () =>
                {
                    var data = repository.GetHighVolumeDeletionSignals(fromUtc, untilUtcExclusive, actor, (int)threshold.Value);
                    grid.DataSource = data;
                    footer.Text = (fa ? "گروه‌های مطابق قانون: " : "Matching groups: ") + data.Rows.Count +
                        (fa ? " — این موارد لزوماً تخلف نیستند." : " — not necessarily suspicious.");
                };
                saveRule.Click += (sender, args) =>
                {
                    try
                    {
                        repository.SaveDailyDeletionThreshold((int)threshold.Value);
                        MessageBox.Show(dialog, fa ? "قانون در دیتابیس ذخیره شد." : "Rule saved in SQL Server.");
                    }
                    catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Save rule", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                search.Click += (sender, args) =>
                {
                    try { evaluate(); }
                    catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Threshold", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                threshold.Value = Math.Max((decimal)threshold.Minimum,
                    Math.Min((decimal)threshold.Maximum, repository.GetSavedDailyDeletionThreshold()));
                evaluate();
                dialog.ShowDialog(this);
            }
        }
    }
}
