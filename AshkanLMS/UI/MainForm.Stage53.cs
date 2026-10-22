using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        private static string SqlTroubleshootingHint(int number, bool fa)
        {
            switch (number)
            {
                case 53:
                case 26:
                case 40:
                    return fa ? "سرور پیدا نشد: نام Instance، سرویس SQL Server، TCP/IP و فایروال را بررسی کنید." :
                        "Server unavailable: check instance name, SQL Server service, TCP/IP and firewall.";
                case 18456:
                    return fa ? "ورود رد شد: حساب Windows/SQL، مجوزها و Authentication Mode را بررسی کنید." :
                        "Login rejected: verify Windows/SQL identity, permissions and authentication mode.";
                case 4060:
                    return fa ? "دیتابیس قابل بازشدن نیست: نام دیتابیس و مجوز کاربر را بررسی کنید." :
                        "Database cannot be opened: verify database name and user access.";
                case -2146893019:
                case -2146893022:
                    return fa ? "مشکل گواهی TLS: زنجیره اعتماد و گواهی سرور را اصلاح کنید؛ در محیط عملیاتی اعتبارسنجی را غیرفعال نکنید." :
                        "TLS certificate issue: fix server certificate and trust chain; do not disable validation in production.";
                case -2:
                    return fa ? "مهلت اتصال تمام شد: سرویس، شبکه و فایروال را بررسی کنید." :
                        "Connection timed out: check service, network and firewall.";
                default:
                    return fa ? "کد خطای SQL را با مستندات SQL Server تطبیق دهید؛ نام سرور و مجوزها را بررسی کنید." :
                        "Look up the SQL error code; verify server configuration and permissions.";
            }
        }

        private void ShowDatabaseHealthCenter()
        {
            AuthorizationService.RequireAccountAdmin();
            bool fa = AppSession.Persian;
            using (var dialog = new Form { Text = fa ? "مرکز سلامت دیتابیس" : "Database Health Center",
                StartPosition = FormStartPosition.CenterParent, Size = new Size(860, 580),
                MinimumSize = new Size(640, 420), BackColor = Color.FromArgb(243,247,252),
                RightToLeft = fa ? RightToLeft.Yes : RightToLeft.No })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 4 };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));
                dialog.Controls.Add(layout);
                var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
                var run = new Button { Text = fa ? "بررسی مجدد" : "Run diagnostics", Width = 145, Height = 34 };
                header.Controls.Add(run);
                var copyReport = new Button { Text = fa ? "کپی گزارش امن" : "Copy safe report", Width = 145, Height = 34 };
                header.Controls.Add(copyReport);
                header.Controls.Add(new Label { Text = fa ? "فقط خواندنی؛ بدون تغییر اطلاعات" : "Read-only checks; no data changes",
                    AutoSize = true, Padding = new Padding(12, 9, 0, 0) });
                layout.Controls.Add(header, 0, 0);
                var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
                layout.Controls.Add(grid, 0, 1);
                var footer = new Label { Dock = DockStyle.Fill, AutoSize = false, ForeColor = Color.FromArgb(70,80,100),
                    Text = fa ? "اتصال موفق، مجوز نوشتن یا صحت همه عملیات را تضمین نمی‌کند." :
                        "A successful connection does not verify write permissions or full application functionality." };
                layout.Controls.Add(footer, 0, 2);
                var guidance = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White, ForeColor = Color.FromArgb(42, 60, 86) };
                layout.Controls.Add(guidance, 0, 3);
                Action check = () =>
                {
                    run.Enabled = false;
                    try
                    {
                        var results = new DataTable();
                        results.Columns.Add(fa ? "بررسی" : "Check");
                        results.Columns.Add(fa ? "وضعیت" : "Status");
                        results.Columns.Add(fa ? "توضیح" : "Details");
                        var mode = ConfigurationManager.AppSettings["PersistenceMode"] ?? "Json";
                        results.Rows.Add("PersistenceMode", "INFO", mode);
                        guidance.Text = fa ? "برای بررسی خطاها روی «بررسی مجدد» کلیک کنید." : "Run diagnostics to see troubleshooting guidance.";
                        if (!string.Equals(mode, "Json", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(mode, "SqlServer", StringComparison.OrdinalIgnoreCase))
                            results.Rows.Add("PersistenceMode", "WARN", fa ? "مقدار نامعتبر؛ Json یا SqlServer را انتخاب کنید" : "Invalid value; choose Json or SqlServer");
                        var entry = ConfigurationManager.ConnectionStrings["AshkanLMS"];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
                            {
                            results.Rows.Add("Connection configuration", "FAIL", "AshkanLMS connection string missing");
                            guidance.Text = fa ? "در App.config بخش connectionStrings، ورودی AshkanLMS را بررسی کنید. رمز را در گزارش ارسال نکنید." :
                                "Check the AshkanLMS entry in App.config / connectionStrings. Never share credentials.";
                        }
                        else
                        {
                            try
                            {
                                var builder = new SqlConnectionStringBuilder(entry.ConnectionString);
                                // Never display the connection string or password.
                                results.Rows.Add("Server", "INFO", builder.DataSource);
                                using (var connection = new SqlConnection(entry.ConnectionString))
                                {
                                    connection.Open();
                                    results.Rows.Add("SQL Server", "PASS", fa ? "اتصال برقرار شد" : "Connection opened");
                                    guidance.Text = fa ? "اتصال برقرار است. اگر جدول MISSING بود، اسکریپت مربوطه را پس از پشتیبان‌گیری اجرا کنید. این ابزار فقط خواندنی است." :
                                        "Connected. For MISSING tables, back up first and apply the corresponding migration script. This tool is read-only.";
                                    using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                                        results.Rows.Add("Database", "PASS", Convert.ToString(command.ExecuteScalar()));
                                    string[] tables = { "LmsState", "LmsStudents", "LmsInstructors", "LmsCourses", "LmsEnrollments",
                                        "LmsRelationalAudit", "LmsAuditReviews", "LmsAuditReviewHistory", "LmsDeletionThresholdRules" };
                                    foreach (string table in tables)
                                    {
                                        using (var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@name, 'U') IS NULL THEN 0 ELSE 1 END", connection))
                                        {
                                            command.Parameters.Add("@name", SqlDbType.NVarChar, 256).Value = "dbo." + table;
                                            bool found = Convert.ToInt32(command.ExecuteScalar()) == 1;
                                            results.Rows.Add("dbo." + table, found ? "PASS" : "MISSING",
                                                found ? (fa ? "موجود" : "Exists") : (fa ? "یافت نشد؛ اسکریپت‌ها را بررسی کنید" : "Not found; check SQL scripts"));
                                        }
                                    }
                                }
                            }
                            catch (SqlException ex)
                            {
                                // No exception text: it may include server or account details.
                                guidance.Text = SqlTroubleshootingHint(ex.Number, fa);
                                results.Rows.Add("SQL Server", "FAIL", "SQL error " + ex.Number +
                                    (fa ? "؛ تنظیمات سرور، گواهی TLS و دسترسی را بررسی کنید" : "; check server, TLS certificate and access"));
                            }
                            catch (Exception ex)
                            {
                                guidance.Text = fa ? "قالب اتصال، نام سرور و دسترسی شبکه را بررسی کنید؛ اطلاعات محرمانه را ارسال نکنید." :
                                    "Check connection-string syntax, server name and network access. Do not share secrets.";
                                results.Rows.Add("Connection", "FAIL", ex.GetType().Name +
                                    (fa ? "؛ تنظیمات اتصال را بررسی کنید" : "; review connection settings"));
                            }
                        }
                        grid.DataSource = results;
                    }
                    finally { run.Enabled = true; }
                };
                copyReport.Click += (sender, args) =>
                {
                    var report = grid.DataSource as DataTable;
                    if (report == null) return;
                    var lines = new System.Collections.Generic.List<string>();
                    lines.Add("AshkanLMS database diagnostics (sanitized)");
                    foreach (DataRow item in report.Rows)
                    {
                        string label = Convert.ToString(item[0]);
                        // Do not copy infrastructure identifiers or connection details.
                        if (label == "Server" || label == "Database" || label == "Connection configuration")
                            continue;
                        string detail = Convert.ToString(item[2]);
                        if (label == "SQL Server" || label == "Connection")
                            detail = "See local diagnostics";
                        lines.Add(label + ": " + Convert.ToString(item[1]) + " — " + detail);
                    }
                    try
                    {
                        Clipboard.SetText(string.Join(Environment.NewLine, lines));
                        MessageBox.Show(fa ? "گزارش بدون نام سرور و دیتابیس کپی شد." :
                            "Report copied without server and database names.", dialog.Text);
                    }
                    catch (System.Runtime.InteropServices.ExternalException)
                    {
                        MessageBox.Show(fa ? "کلیپ‌بورد در دسترس نیست؛ دوباره تلاش کنید." :
                            "Clipboard unavailable. Please retry.", dialog.Text);
                    }
                };
                run.Click += (sender, args) => check();
                check();
                dialog.ShowDialog(this);
            }
        }
    }
}
