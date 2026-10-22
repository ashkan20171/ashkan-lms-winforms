using System;
using System.Threading;
using System.Windows.Forms;
using AshkanLMS.Core;
using AshkanLMS.Services;
using AshkanLMS.UI;

namespace AshkanLMS
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnUiException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
            try
            {
                Localization.Load();
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                CrashReportService.Report(ex, "UI");
            }
        }

        private static void OnUiException(object sender, ThreadExceptionEventArgs args)
        {
            CrashReportService.Report(args.Exception, "UI");
        }

        private static void OnDomainException(object sender, UnhandledExceptionEventArgs args)
        {
            CrashReportService.Report(args.ExceptionObject as Exception, "Background");
            // CLR may terminate the process for an unhandled background exception.
        }
    }
}
