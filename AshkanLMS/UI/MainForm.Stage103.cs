using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Search and paging are performed over current in-memory records; no changes to persistence.
        private void AttachStudentSearch103(Panel card, DataGridView grid)
        {
            AttachAcademicBrowser103(card, grid, true);
        }

        private void AttachInstructorSearch103(Panel card, DataGridView grid)
        {
            AttachAcademicBrowser103(card, grid, false);
        }

        private void AttachAcademicBrowser103(Panel card, DataGridView grid, bool students)
        {
            const int pageSize = 20;
            int page = 0;
            var search = new TextBox { Left = 400, Top = 58, Width = 215, Font = Theme.Font(9) };
            var previous = Theme.Button(AppSession.Persian ? "قبلی" : "Previous");
            var next = Theme.Button(AppSession.Persian ? "بعدی" : "Next");
            var count = Theme.Label("", 9, FontStyle.Regular);
            previous.SetBounds(625, 58, 85, 32);
            next.SetBounds(716, 58, 85, 32);
            count.SetBounds(808, 61, 160, 28);
            search.RightToLeft = AppSession.Persian ? RightToLeft.Yes : RightToLeft.No;
            card.Controls.AddRange(new Control[] { search, previous, next, count });
            Action refresh = () =>
            {
                string query = search.Text.Trim();
                if (students)
                {
                    var records = data.Students.Where(x => query.Length == 0 ||
                        (x.Name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (x.Email ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        x.Id.ToString().Contains(query)).OrderBy(x => x.Id).ToList();
                    int pages = Math.Max(1, (records.Count + pageSize - 1) / pageSize);
                    page = Math.Min(page, pages - 1);
                    grid.DataSource = records.Skip(page * pageSize).Take(pageSize).Select(x =>
                        new StudentRow { ID = x.Id, Name = x.Name, Email = x.Email,
                            Progress = x.Progress + "%", Status = Localization.T(x.StatusKey) }).ToList();
                    SetHeaders(grid, new[] { "Id", "Name", "Email", "Progress", "Status" });
                    count.Text = (page + 1) + " / " + pages + " (" + records.Count + ")";
                    next.Enabled = page + 1 < pages;
                }
                else
                {
                    var records = data.Instructors.Where(x => query.Length == 0 ||
                        (x.Name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (x.Email ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (x.Expertise ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        x.Id.ToString().Contains(query)).OrderBy(x => x.Id).ToList();
                    int pages = Math.Max(1, (records.Count + pageSize - 1) / pageSize);
                    page = Math.Min(page, pages - 1);
                    grid.DataSource = records.Skip(page * pageSize).Take(pageSize).Select(x =>
                        new InstructorBrowseRow103 { Id = x.Id, Name = x.Name, Expertise = x.Expertise,
                            Email = x.Email, Courses = data.Courses.Count(c => c.Instructor == x.Name) }).ToList();
                    count.Text = (page + 1) + " / " + pages + " (" + records.Count + ")";
                    next.Enabled = page + 1 < pages;
                }
                previous.Enabled = page > 0;
                grid.ClearSelection();
            };
            search.TextChanged += (sender, args) => { page = 0; refresh(); };
            previous.Click += (sender, args) => { if (page > 0) { page--; refresh(); } };
            next.Click += (sender, args) => { page++; refresh(); };
            refresh();
        }

        private sealed class InstructorBrowseRow103
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Expertise { get; set; }
            public string Email { get; set; }
            public int Courses { get; set; }
        }
    }
}
