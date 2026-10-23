using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Editable academic profiles with persisted contact details.
        private void AttachAcademicProfiles104(Panel card, DataGridView grid, bool student)
        {
            var details = Theme.Button(AppSession.Persian ? "مشاهده پروفایل" : "View profile");
            details.SetBounds(student ? 240 : 390, student ? 14 : 55, 145, 36);
            details.Click += (sender, args) =>
            {
                if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow)
                {
                    MessageBox.Show(this, AppSession.Persian ? "ابتدا یک رکورد انتخاب کنید." : "Select a record first.");
                    return;
                }
                var cell = grid.CurrentRow.Cells.Cast<DataGridViewCell>()
                    .FirstOrDefault(c => string.Equals(c.OwningColumn.Name, "ID", StringComparison.OrdinalIgnoreCase));
                int id;
                if (cell == null || cell.Value == null || !int.TryParse(cell.Value.ToString(), out id)) return;
                ShowAcademicProfile104(id, student);
            };
            card.Controls.Add(details);
            grid.CellDoubleClick += (sender, args) =>
            {
                if (args.RowIndex >= 0) details.PerformClick();
            };
        }

        private void ShowAcademicProfile104(int id, bool student)
        {
            var st = student ? data.Students.FirstOrDefault(x => x.Id == id) : null;
            var ins = !student ? data.Instructors.FirstOrDefault(x => x.Id == id) : null;
            if (student && st == null || !student && ins == null)
            {
                MessageBox.Show(this, AppSession.Persian ? "رکورد یافت نشد." : "Record not found.");
                return;
            }
            bool fa = AppSession.Persian;
            string name = student ? st.Name : ins.Name;
            string email = student ? st.Email : ins.Email;
            using (var dialog = new Form
            {
                Text = fa ? "پروفایل آموزشی" : "Academic profile",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(670, 530), MinimumSize = new Size(550, 440),
                Font = Theme.Font(10), BackColor = Theme.Canvas,
                RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No,
                RightToLeftLayout = fa
            })
            {
                var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 7) };
                var overview = new TabPage(fa ? "مشخصات" : "Overview") { BackColor = Theme.Card };
                var related = new TabPage(fa ? "سوابق و دوره‌ها" : "Courses & history") { BackColor = Theme.Card };
                tabs.TabPages.Add(overview);
                tabs.TabPages.Add(related);
                var fields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 7, AutoScroll = true };
                fields.Controls.Add(ProfileLabel104((fa ? "شناسه: " : "ID: ") + id));
                fields.Controls.Add(ProfileLabel104((fa ? "نام: " : "Name: ") + (name ?? "")));
                fields.Controls.Add(ProfileLabel104((fa ? "ایمیل: " : "Email: ") + (email ?? "")));
                fields.Controls.Add(ProfileLabel104(student ? (fa ? "پیشرفت: " : "Progress: ") + st.Progress + "%" : (fa ? "تخصص: " : "Expertise: ") + (ins.Expertise ?? "")));
                fields.Controls.Add(ProfileLabel104(student ? (fa ? "وضعیت: " : "Status: ") + Localization.T(st.StatusKey) : (fa ? "تعداد دوره‌های مرتبط: " : "Related courses: ") + data.Courses.Count(x => x.InstructorId == id || (x.InstructorId == 0 && data.Instructors.Count(i => string.Equals(i.Name, x.Instructor, StringComparison.OrdinalIgnoreCase)) == 1 && string.Equals(x.Instructor, name, StringComparison.OrdinalIgnoreCase)))));
                overview.Controls.Add(fields);
                fields.Controls.Add(ProfileLabel104((fa ? "تلفن: " : "Phone: ") + (student ? st.Phone : ins.Phone)));
                fields.Controls.Add(ProfileLabel104((fa ? "آدرس: " : "Address: ") + (student ? st.Address : ins.Address)));
                var editContact = Theme.Button(fa ? "ویرایش اطلاعات تماس" : "Edit contact details");
                editContact.Dock = DockStyle.Bottom;
                editContact.Height = 42;
                editContact.Click += (sender, args) =>
                {
                    using (var editor = new Form { Text = fa ? "اطلاعات تماس" : "Contact details", StartPosition = FormStartPosition.CenterParent,
                        Width = 460, Height = 260, Font = Theme.Font(10), BackColor = Theme.Canvas,
                        RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = fa })
                    {
                        var formFields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 5 };
                        var phoneBox = new TextBox { Text = (student ? st.Phone : ins.Phone) ?? "", Dock = DockStyle.Fill, MaxLength = 32 };
                        var addressBox = new TextBox { Text = (student ? st.Address : ins.Address) ?? "", Dock = DockStyle.Fill, MaxLength = 400 };
                        var saveButton = Theme.Button(fa ? "ذخیره" : "Save");
                        formFields.Controls.Add(ProfileLabel104(fa ? "شماره تماس" : "Phone"));formFields.Controls.Add(phoneBox);
                        formFields.Controls.Add(ProfileLabel104(fa ? "آدرس" : "Address"));formFields.Controls.Add(addressBox);
                        formFields.Controls.Add(saveButton);
                        saveButton.Click += (s2, a2) => { try { data.UpdateAcademicContact(id, student, phoneBox.Text, addressBox.Text); editor.DialogResult = DialogResult.OK; editor.Close(); }
                            catch (Exception ex) { MessageBox.Show(editor, ex.Message, fa ? "خطا" : "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); } };
                        editor.Controls.Add(formFields);
                        if (editor.ShowDialog(dialog) == DialogResult.OK) { dialog.Close(); ShowAcademicProfile104(id, student); }
                    }
                };
                overview.Controls.Add(editContact);

                var grid = Grid(); grid.Dock = DockStyle.Fill;
                if (student)
                {
                    grid.DataSource = data.Enrollments.Where(x => x.StudentId == id || (x.StudentId == 0 && data.Students.Count(i => string.Equals(i.Name, x.Student, StringComparison.OrdinalIgnoreCase)) == 1 && string.Equals(x.Student, name, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(x => x.Date).Select(x => new { x.Id, x.Course, x.Date, Status = Localization.T(x.StatusKey) }).ToList();
                }
                else
                {
                    grid.DataSource = data.Courses.Where(x => x.InstructorId == id || (x.InstructorId == 0 && data.Instructors.Count(i => string.Equals(i.Name, x.Instructor, StringComparison.OrdinalIgnoreCase)) == 1 && string.Equals(x.Instructor, name, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(x => x.Title).Select(x => new { x.Id, x.Title, x.Students, x.Progress }).ToList();
                }
                related.Controls.Add(grid);
                var footer = new Label
                {
                    Dock = DockStyle.Bottom, Height = 36, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Theme.Muted, BackColor = Theme.Canvas,
                    Text = fa ? "اطلاعات تماس قابل ویرایش است." : "Contact details can be edited."
                };
                dialog.Controls.Add(tabs); dialog.Controls.Add(footer);
                dialog.ShowDialog(this);
            }
        }
        private Label ProfileLabel104(string value)
        {
            return new Label { Text = value, Dock = DockStyle.Fill, AutoEllipsis = true,
                TextAlign = AppSession.Persian ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                ForeColor = Theme.Text, Font = Theme.Font(10) };
        }
    }
}
