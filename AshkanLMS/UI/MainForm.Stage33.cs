using System;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows.Forms;
using AshkanLMS.Services;
using AshkanLMS.Core;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Explicit opt-in workspace: the legacy screens are deliberately unchanged until cutover.
        void ShowRelationalWorkspace()
        {
            Clear();
            var persian = AppSession.Persian;
            var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), AutoScroll = true, BackColor = Theme.Canvas };
            content.Controls.Add(host);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            host.Controls.Add(layout);
            var heading = new Label { Text = persian ? "محیط SQL Server رابطه‌ای (آزمایشی)" : "Relational SQL Server workspace (opt-in)", Dock = DockStyle.Fill, Font = Theme.Font(15, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(heading, 0, 0);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true };
            layout.Controls.Add(toolbar, 0, 1);
            var grid = Grid(); grid.Dock = DockStyle.Fill; grid.AutoGenerateColumns = true;
            layout.Controls.Add(grid, 0, 2);
            RelationalCoreRepository repo;
            try
            {
                var connection = ConfigurationManager.ConnectionStrings["AshkanLMS"];
                if (connection == null) throw new InvalidOperationException("Missing AshkanLMS connection string.");
                repo = new RelationalCoreRepository(connection.ConnectionString);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
            string view = "Students";
            string auditEntity = null, auditActor = null;
            DateTime? auditFrom = null, auditUntil = null;
            System.Data.DataTable currentAudit = null;
            Action reload = () => { try { grid.DataSource = view == "Students" ? (object)repo.GetStudents() : view == "Courses" ? (object)repo.GetCourses() : view == "Enrollments" ? (object)repo.GetEnrollments() : view == "Audit" ? (object)(currentAudit = repo.SearchAuditHistory(auditEntity, auditActor, auditFrom, auditUntil)) : repo.GetInstructors(); } catch (Exception ex) { MessageBox.Show(ex.Message, "SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error); } };
            Action<string, Action> add = (label, action) => { var b = Theme.Button(label); b.AutoSize = true; b.Click += (s,e) => { try { action(); } catch (Exception ex) { MessageBox.Show(ex.Message, "SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error); } }; toolbar.Controls.Add(b); };
            add(persian ? "دانشجویان" : "Students", () => { view = "Students"; reload(); });
            add(persian ? "دوره‌ها" : "Courses", () => { view = "Courses"; reload(); });
            add(persian ? "ثبت‌نام‌ها" : "Enrollments", () => { view = "Enrollments"; reload(); });
            add(persian ? "اساتید" : "Instructors", () => { view = "Instructors"; reload(); });
            add(persian ? "تاریخچه تغییرات" : "Audit history", () => { view = "Audit"; reload(); });
            add(persian ? "بازخوانی" : "Refresh", () => reload());
            add(persian ? "داشبورد ممیزی" : "Audit dashboard", () => ShowAuditDashboard(repo, persian));
            add(persian ? "فیلتر تاریخچه" : "Filter audit", () =>
            {
                if (view != "Audit") { MessageBox.Show("Select Audit history first."); return; }
                var entity = PromptRelational(persian ? "نوع موجودیت (Student/Course/Instructor/Enrollment، خالی=همه)" : "Entity (blank = all)", auditEntity ?? "");
                if (entity == null) return;
                var actor = PromptRelational(persian ? "نام کاربر (خالی=همه)" : "Actor (blank = all)", auditActor ?? "");
                if (actor == null) return;
                var from = PromptRelational("From UTC (yyyy-MM-dd, blank = all)", auditFrom.HasValue ? auditFrom.Value.ToString("yyyy-MM-dd") : "");
                if (from == null) return;
                var until = PromptRelational("Through UTC date (yyyy-MM-dd, blank = all)", auditUntil.HasValue ? auditUntil.Value.AddDays(-1).ToString("yyyy-MM-dd") : "");
                if (until == null) return;
                DateTime parsedFrom = DateTime.MinValue, parsedUntil = DateTime.MinValue;
                if (!string.IsNullOrWhiteSpace(from) && !DateTime.TryParseExact(from.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedFrom)) throw new ArgumentException("Invalid From UTC date.");
                if (!string.IsNullOrWhiteSpace(until) && !DateTime.TryParseExact(until.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedUntil)) throw new ArgumentException("Invalid Through UTC date.");
                auditEntity = string.IsNullOrWhiteSpace(entity) ? null : entity.Trim();
                auditActor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
                auditFrom = string.IsNullOrWhiteSpace(from) ? (DateTime?)null : parsedFrom;
                auditUntil = string.IsNullOrWhiteSpace(until) ? (DateTime?)null : parsedUntil.AddDays(1);
                reload();
            });
            add(persian ? "خروجی CSV تاریخچه" : "Export audit CSV", () =>
            {
                if (view != "Audit" || currentAudit == null) { MessageBox.Show("Load Audit history first."); return; }
                AuthorizationService.RequireAccountAdmin();
                using (var dialog = new SaveFileDialog { Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = "ashkanlms-audit.csv" })
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    var builder = new StringBuilder();
                    builder.AppendLine(string.Join(",", currentAudit.Columns.Cast<System.Data.DataColumn>().Select(c => CsvAuditCell(c.ColumnName))));
                    foreach (System.Data.DataRow auditRow in currentAudit.Rows)
                        builder.AppendLine(string.Join(",", auditRow.ItemArray.Select(v => CsvAuditCell(v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.InvariantCulture)))));
                    File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(true));
                }
            });

            add(persian ? "افزودن دانشجو" : "Add student", () =>
            {
                if (view != "Students") { MessageBox.Show("Select Students first."); return; }
                var name = PromptRelational("Student name", ""); if (name == null) return;
                var email = PromptRelational("Email", ""); if (email == null) return;
                var id = PromptRelational("Student ID (positive integer)", ""); if (id == null) return;
                int number; if (!int.TryParse(id, out number) || number <= 0) throw new ArgumentException("Invalid student ID.");
                repo.InsertStudent(new AshkanLMS.Models.Student { Id = number, Name = name.Trim(), Email = email.Trim(), StatusKey = "Active", Progress = 0 }); reload();
            });
            add(persian ? "ویرایش دانشجو" : "Edit student", () =>
            {
                if (view != "Students" || grid.CurrentRow == null) return;
                var student = grid.CurrentRow.DataBoundItem as AshkanLMS.Models.Student; if (student == null) return;
                var originalVersion = repo.GetStudentVersion(student.Id);
                var name = PromptRelational("Student name", student.Name); if (name == null) return;
                var email = PromptRelational("Email", student.Email); if (email == null) return;
                try { repo.UpdateStudentChecked(new AshkanLMS.Models.Student { Id = student.Id, Name = name.Trim(), Email = email.Trim(), StatusKey = student.StatusKey, Progress = student.Progress }, originalVersion); }
                catch (System.Data.DBConcurrencyException) { ShowRelationalConflictDetails(persian, "Student #" + student.Id,
                    new [] { "Name: " + student.Name, "Email: " + student.Email },
                    new [] { "Name: " + name, "Email: " + email },
                    repo.GetStudents().Where(x => x.Id == student.Id).Select(x => "Name: " + x.Name + "\nEmail: " + x.Email).FirstOrDefault()); } reload();
            });
            add(persian ? "حذف دانشجو" : "Delete student", () =>
            {
                if (view != "Students" || grid.CurrentRow == null) return;
                var student = grid.CurrentRow.DataBoundItem as AshkanLMS.Models.Student; if (student == null) return;
                if (MessageBox.Show("Delete student #" + student.Id + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                repo.DeleteStudent(student.Id); reload();
            });
            add(persian ? "افزودن دوره" : "Add course", () =>
            {
                if (view != "Courses") { MessageBox.Show("Select Courses first."); return; }
                var id = PromptRelational("Course ID", ""); if (id == null) return;
                int number; if (!int.TryParse(id, out number) || number <= 0) throw new ArgumentException("Invalid course ID.");
                var title = PromptRelational("Course title", ""); if (title == null) return;
                repo.InsertCourse(number, title.Trim(), null); reload();
            });
            add(persian ? "حذف دوره" : "Delete course", () =>
            {
                if (view != "Courses" || grid.CurrentRow == null) return;
                var course = grid.CurrentRow.DataBoundItem as AshkanLMS.Models.Course; if (course == null) return;
                if (MessageBox.Show("Delete course #" + course.Id + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                repo.DeleteCourse(course.Id); reload();
            });
            add(persian ? "ثبت‌نام جدید" : "New enrollment", () =>
            {
                if (view != "Enrollments") { MessageBox.Show(persian ? "ابتدا ثبت‌نام‌ها را انتخاب کنید." : "Select Enrollments first."); return; }
                var a = PromptRelational(persian ? "شناسه ثبت‌نام" : "Enrollment ID", ""); if (a == null) return;
                int id; if (!int.TryParse(a, out id) || id <= 0) throw new ArgumentException("Invalid enrollment ID.");
                var studentId = SelectRelationalId(repo.GetStudents().Select(x => new RelationalChoice { Id = x.Id, Label = x.Name + " (#" + x.Id + ")" }).ToList(), persian ? "انتخاب دانشجو" : "Select student", false);
                if (!studentId.HasValue) return;
                var courseId = SelectRelationalId(repo.GetCourses().Select(x => new RelationalChoice { Id = x.Id, Label = x.Title + " (#" + x.Id + ")" }).ToList(), persian ? "انتخاب دوره" : "Select course", false);
                if (!courseId.HasValue) return;
                repo.Enroll(id, studentId.Value, courseId.Value, DateTime.UtcNow); reload();
            });
            add(persian ? "تغییر وضعیت ثبت‌نام" : "Enrollment status", () =>
            {
                if (view != "Enrollments" || grid.CurrentRow == null) return;
                var row = grid.CurrentRow.DataBoundItem as System.Data.DataRowView; if (row == null) return;
                var version = repo.GetEnrollmentVersion(Convert.ToInt32(row["EnrollmentId"]));
                var status = PromptRelational(persian ? "وضعیت جدید" : "New status", Convert.ToString(row["StatusKey"])); if (status == null) return;
                if (string.IsNullOrWhiteSpace(status) || status.Length > 40) throw new ArgumentException("Invalid status.");
                try { repo.SetEnrollmentStatusChecked(Convert.ToInt32(row["EnrollmentId"]), status.Trim(), version); }
                catch (System.Data.DBConcurrencyException) { ShowRelationalConflictDetails(persian, "Enrollment #" + row["EnrollmentId"],
                    new [] { "Status: " + Convert.ToString(row["StatusKey"]) }, new [] { "Status: " + status },
                    FindRelationalRow(repo.GetEnrollments(), "EnrollmentId", Convert.ToInt32(row["EnrollmentId"]), "StatusKey")); } reload();
            });
            add(persian ? "حذف ثبت‌نام" : "Remove enrollment", () =>
            {
                if (view != "Enrollments" || grid.CurrentRow == null) return;
                var row = grid.CurrentRow.DataBoundItem as System.Data.DataRowView; if (row == null) return;
                var id = Convert.ToInt32(row["EnrollmentId"]);
                if (MessageBox.Show("Remove enrollment #" + id + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                repo.RemoveEnrollment(id); reload();
            });
            add(persian ? "انتخاب استاد دوره" : "Assign instructor", () =>
            {
                if (view != "Courses" || grid.CurrentRow == null) { MessageBox.Show("Select a course first."); return; }
                var course = grid.CurrentRow.DataBoundItem as AshkanLMS.Models.Course; if (course == null) return;
                var version = repo.GetCourseVersion(course.Id);
                var instructor = SelectRelationalId(repo.GetInstructors().Rows.Cast<System.Data.DataRow>().Select(r =>
                    new RelationalChoice { Id = Convert.ToInt32(r["InstructorId"]), Label = Convert.ToString(r["FullName"]) + " (#" + r["InstructorId"] + ")" }).ToList(),
                    persian ? "انتخاب استاد" : "Select instructor", true);
                if (!instructor.HasValue) return;
                int? selected = instructor.Value == 0 ? (int?)null : instructor.Value;
                try { repo.SetCourseInstructorChecked(course.Id, selected, version); }
                catch (System.Data.DBConcurrencyException) { ShowRelationalConflictDetails(persian, "Course #" + course.Id,
                    new [] { "Instructor (previous view): " + course.Instructor },
                    new [] { "Selected instructor ID: " + (selected.HasValue ? selected.Value.ToString() : "None") },
                    repo.GetCourses().Where(x => x.Id == course.Id).Select(x => "Instructor (current): " + x.Instructor).FirstOrDefault()); } reload();
            });
            add(persian ? "افزودن استاد" : "Add instructor", () =>
            {
                if (view != "Instructors") return;
                var idText = PromptRelational(persian ? "شناسه استاد" : "Instructor ID", ""); if (idText == null) return;
                int id; if (!int.TryParse(idText, out id)) throw new ArgumentException("Invalid ID.");
                var name = PromptRelational(persian ? "نام استاد" : "Instructor name", ""); if (name == null) return;
                var email = PromptRelational("Email", ""); if (email == null) return;
                var expertise = PromptRelational(persian ? "تخصص" : "Expertise", ""); if (expertise == null) return;
                repo.InsertInstructor(id, name, email, expertise); reload();
            });
            add(persian ? "ویرایش استاد" : "Edit instructor", () =>
            {
                if (view != "Instructors" || grid.CurrentRow == null) return;
                var row = grid.CurrentRow.DataBoundItem as System.Data.DataRowView; if (row == null) return;
                int id = Convert.ToInt32(row["InstructorId"]);
                var version = repo.GetInstructorVersion(id);
                var name = PromptRelational(persian ? "نام استاد" : "Instructor name", Convert.ToString(row["FullName"])); if (name == null) return;
                var email = PromptRelational("Email", Convert.ToString(row["Email"])); if (email == null) return;
                var expertise = PromptRelational(persian ? "تخصص" : "Expertise", ""); if (expertise == null) return;
                try { repo.UpdateInstructorChecked(id, name, email, expertise, version); }
                catch (System.Data.DBConcurrencyException) { ShowRelationalConflictDetails(persian, "Instructor #" + id,
                    new [] { "Name: " + Convert.ToString(row["FullName"]), "Email: " + Convert.ToString(row["Email"]), "Expertise: " + Convert.ToString(row["Expertise"]) },
                    new [] { "Name: " + name, "Email: " + email, "Expertise: " + expertise },
                    FindRelationalRow(repo.GetInstructors(), "InstructorId", id, "FullName", "Email", "Expertise")); } reload();
            });
            add(persian ? "حذف استاد" : "Delete instructor", () =>
            {
                if (view != "Instructors" || grid.CurrentRow == null) return;
                var row = grid.CurrentRow.DataBoundItem as System.Data.DataRowView; if (row == null) return;
                int id = Convert.ToInt32(row["InstructorId"]);
                if (MessageBox.Show((persian ? "حذف استاد شماره " : "Delete instructor #") + id + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                repo.DeleteInstructor(id); reload();
            });
            reload();
        }
        private static string CsvAuditCell(string value)
        {
            value = value ?? "";
            // Prevent spreadsheet formula execution when CSV is opened in Excel.
            if (value.Length > 0 && (value[0] == '=' || value[0] == '+' || value[0] == '-' || value[0] == '@' || value[0] == '\t' || value[0] == '\r')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string FindRelationalRow(System.Data.DataTable table, string idColumn, int id, params string[] columns)
        {
            foreach (System.Data.DataRow row in table.Rows)
                if (Convert.ToInt32(row[idColumn]) == id)
                    return string.Join("\n", columns.Select(column => column + ": " + Convert.ToString(row[column])).ToArray());
            return null;
        }
        // Read-only comparison: never automatically overwrite another user's changes.
        private static void ShowRelationalConflictDetails(bool persian, string record,
            string[] original, string[] attempted, string current)
        {
            using (var dialog = new Form { Text = persian ? "مقایسه تداخل ویرایش" : "Concurrent edit comparison",
                Width = 780, Height = 480, StartPosition = FormStartPosition.CenterParent,
                MinimumSize = new Size(600, 350), RightToLeft = persian ? RightToLeft.Yes : RightToLeft.No,
                RightToLeftLayout = persian })
            {
                var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 3, RowCount = 3 };
                for (int i = 0; i < 3; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
                var notice = new Label { Dock = DockStyle.Fill, AutoSize = false, Text = record + "\n" +
                    (persian ? "اطلاعات تغییر کرده است. برای جلوگیری از حذف تغییرات دیگران، ذخیره انجام نشد." :
                     "The record changed. Your save was rejected to protect another user's edits."),
                    ForeColor = Color.DarkRed, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                panel.Controls.Add(notice, 0, 0); panel.SetColumnSpan(notice, 3);
                string[] headings = persian ? new [] { "هنگام باز کردن", "تغییرات شما (ذخیره نشده)", "آخرین اطلاعات دیتابیس" } :
                    new [] { "Previously displayed", "Your unsaved changes", "Latest database values" };
                string[] values = { string.Join("\n", original), string.Join("\n", attempted),
                    current ?? (persian ? "رکورد حذف شده است" : "Record was deleted") };
                for (int i = 0; i < 3; i++)
                {
                    var group = new GroupBox { Text = headings[i], Dock = DockStyle.Fill };
                    var field = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                        Dock = DockStyle.Fill, Text = values[i], Font = new Font("Segoe UI", 10) };
                    group.Controls.Add(field); panel.Controls.Add(group, i, 1);
                }
                var close = new Button { Text = persian ? "بستن و بازخوانی" : "Close and refresh", Dock = DockStyle.Right,
                    Width = 170, DialogResult = DialogResult.OK };
                panel.Controls.Add(close, 2, 2); dialog.Controls.Add(panel); dialog.AcceptButton = close;
                dialog.ShowDialog();
            }
        }
        private static void ShowRelationalConflict(bool persian)
        {
            MessageBox.Show(persian
                ? "این رکورد توسط کاربر دیگری تغییر کرده یا حذف شده است. اطلاعات تازه بارگذاری می‌شوند؛ لطفاً تغییرات خود را پس از بررسی دوباره وارد کنید."
                : "This record was changed or deleted by another user. Latest data will be loaded; review it before editing again.",
                persian ? "تداخل ویرایش" : "Concurrent edit detected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private sealed class RelationalChoice
        {
            internal int Id;
            internal string Label;
            public override string ToString() { return Label; }
        }
        private static int? SelectRelationalId(System.Collections.Generic.List<RelationalChoice> choices, string title, bool allowNone)
        {
            using (var dialog = new Form { Text = title, Width = 430, Height = 165, StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                var combo = new ComboBox { Left = 16, Top = 18, Width = 390, DropDownStyle = ComboBoxStyle.DropDownList };
                if (allowNone) choices.Insert(0, new RelationalChoice { Id = 0, Label = "— None / بدون استاد —" });
                foreach (var choice in choices) combo.Items.Add(choice);
                if (combo.Items.Count == 0) { MessageBox.Show("No records available."); return null; }
                combo.SelectedIndex = 0;
                var ok = new Button { Text = "OK", Left = 316, Top = 65, Width = 90, DialogResult = DialogResult.OK };
                dialog.Controls.Add(combo); dialog.Controls.Add(ok); dialog.AcceptButton = ok;
                return dialog.ShowDialog() == DialogResult.OK ? ((RelationalChoice)combo.SelectedItem).Id : (int?)null;
            }
        }
        static string PromptRelational(string title, string initial)
        {
            using (var form = new Form { Text = title, Width = 440, Height = 170, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                var input = new TextBox { Left = 16, Top = 18, Width = 390, Text = initial };
                var ok = new Button { Text = "OK", Left = 316, Top = 60, Width = 90, DialogResult = DialogResult.OK };
                form.Controls.Add(input); form.Controls.Add(ok); form.AcceptButton = ok;
                return form.ShowDialog() == DialogResult.OK ? input.Text : null;
            }
        }
    }
}
