using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.Services
{
    /// <summary>Minimal diagnostic events. Never records exception messages, stack traces,
    /// credentials, connection strings, user names or machine-specific paths.</summary>
    internal static class CrashReportService
    {
        private static int handling;
        private static readonly object Sync = new object();

        internal static void Report(Exception error, string source)
        {
            if (Interlocked.Exchange(ref handling, 1) != 0) return;
            try
            {
                string type = error == null ? "Unknown" : SafeType(error.GetType().Name);
                string category = source == "UI" ? "UI" : "Background";
                try
                {
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLMS", "Diagnostics");
                    Directory.CreateDirectory(folder);
                    string path = Path.Combine(folder, "events.log");
                    lock (Sync)
                    {
                        // Rotate at 512 KiB; only one prior log retained.
                        if (File.Exists(path) && new FileInfo(path).Length > 524288)
                        {
                            string previous = Path.Combine(folder, "events.previous.log");
                            if (File.Exists(previous)) File.Delete(previous);
                            File.Move(path, previous);
                        }
                        File.AppendAllText(path, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + " | " + category + " | " + type + Environment.NewLine, Encoding.UTF8);
                    }
                }
                catch (IOException) { /* Diagnostics must never crash the application. */ }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }

                try
                {
                    bool persian = AppSession.Persian;
                    MessageBox.Show(
                        persian ? "خطای غیرمنتظره‌ای رخ داد. برنامه ممکن است نیاز به راه‌اندازی مجدد داشته باشد. برای بررسی، به پشتیبانی اطلاع دهید." :
                                  "An unexpected error occurred. The application may need to restart. Please contact support if the issue continues.",
                        "AshkanLMS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (InvalidOperationException) { }
            }
            finally { Interlocked.Exchange(ref handling, 0); }
        }

        private static string SafeType(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            var result = new StringBuilder();
            foreach (char c in name)
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_') result.Append(c);
            return result.Length == 0 ? "Unknown" : result.ToString();
        }
    }
}
