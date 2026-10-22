using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private void ShowAuditReviewCenter(RelationalCoreRepository repository, DateTime fromUtc, DateTime untilUtc, string actor, bool fa)
        {
            AuthorizationService.RequireAccountAdmin();
            using (var window = new Form { Text = fa ? "مرکز رسیدگی به رویدادها" : "Audit review center",
                Size = new Size(1050, 650), MinimumSize = new Size(760, 460), StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(244, 247, 251) })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 3 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
                window.Controls.Add(layout);
                var heading = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Text = fa ? "حذف‌های ثبت‌شده — تصمیم‌های بررسی مستقل از Audit اصلی" : "Recorded deletions — reviews do not change source audit records",
                    ForeColor = Color.FromArgb(32, 55, 84), TextAlign = ContentAlignment.MiddleLeft };
                layout.Controls.Add(heading, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = false,
                    AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                    BackgroundColor = Color.White };
                layout.Controls.Add(grid, 0, 1);
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
                layout.Controls.Add(buttons, 0, 2);
                var refresh = new Button { Text = fa ? "بازخوانی" : "Refresh", Width = 110, Height = 34 };
                var inspect = new Button { Text = fa ? "جزئیات" : "Details", Width = 110, Height = 34 };
                var decide = new Button { Text = fa ? "ثبت نتیجه بررسی" : "Record decision", Width = 150, Height = 34 };
                buttons.Controls.Add(refresh); buttons.Controls.Add(inspect); buttons.Controls.Add(decide);
                Action load = () =>
                {
                    var data = repository.GetAuditReviewDecisions(fromUtc, untilUtc, actor);
                    grid.DataSource = data;
                    foreach (var hidden in new[] { "OldValues", "RowVersion" })
                        if (grid.Columns.Contains(hidden)) grid.Columns[hidden].Visible = false;
                    heading.Text = (fa ? "حذف‌های قابل بررسی: " : "Deletion events: ") + data.Rows.Count +
                        (fa ? " (حداکثر ۳۰۰ مورد اخیر)" : " (latest 300 maximum)");
                };
                Func<DataRowView> selected = () => grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
                inspect.Click += (sender, args) =>
                {
                    var row = selected(); if (row == null) return;
                    using (var details = new Form { Text = fa ? "جزئیات حذف" : "Deletion details", Size = new Size(680, 460), StartPosition = FormStartPosition.CenterParent })
                    {
                        details.Controls.Add(new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true,
                            ScrollBars = ScrollBars.Both, Text = Convert.ToString(row["OldValues"]) });
                        details.ShowDialog(window);
                    }
                };
                grid.CellDoubleClick += (sender, args) => { if (args.RowIndex >= 0) inspect.PerformClick(); };
                decide.Click += (sender, args) =>
                {
                    var row = selected(); if (row == null) return;
                    using (var dialog = new Form { Text = fa ? "نتیجه بررسی" : "Review decision", Size = new Size(560, 330), StartPosition = FormStartPosition.CenterParent })
                    {
                        var body = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 4 };
                        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
                        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
                        dialog.Controls.Add(body);
                        body.Controls.Add(new Label { Text = (fa ? "شناسه رویداد: " : "Audit event: ") + row["AuditId"], Dock = DockStyle.Fill }, 0, 0);
                        var statuses = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
                        statuses.Items.AddRange(new object[] { "Open", "Investigating", "Resolved", "FalsePositive" });
                        string previous = row["ReviewStatus"] == DBNull.Value ? "Open" : Convert.ToString(row["ReviewStatus"]);
                        statuses.SelectedItem = previous;
                        body.Controls.Add(statuses, 0, 1);
                        var note = new TextBox { Dock = DockStyle.Fill, Multiline = true, MaxLength = 2000,
                            ScrollBars = ScrollBars.Vertical, Text = row["ReviewNote"] == DBNull.Value ? "" : Convert.ToString(row["ReviewNote"]) };
                        body.Controls.Add(note, 0, 2);
                        var save = new Button { Text = fa ? "ذخیره" : "Save", Dock = DockStyle.Right, Width = 120 };
                        body.Controls.Add(save, 0, 3);
                        save.Click += (s, e) =>
                        {
                            try
                            {
                                var version = row["RowVersion"] == DBNull.Value ? null : (byte[])row["RowVersion"];
                                repository.SaveAuditReview(Convert.ToInt64(row["AuditId"]), Convert.ToString(statuses.SelectedItem), note.Text, version);
                                dialog.DialogResult = DialogResult.OK;
                            }
                            catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Audit review", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                        };
                        if (dialog.ShowDialog(window) == DialogResult.OK) load();
                    }
                };
                refresh.Click += (sender, args) => { try { load(); } catch (Exception ex) { MessageBox.Show(window, ex.Message); } };
                try { load(); window.ShowDialog(this); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Audit review", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
