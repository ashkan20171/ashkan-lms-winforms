using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLMS.Services;

namespace AshkanLMS.Tests
{
    internal static class Program
    {
        private static int passed;
        private static void Check(string name, bool ok)
        {
            if (!ok) throw new Exception("FAILED: " + name);
            passed++;
            Console.WriteLine("PASS " + name);
        }
        private static DiagnosticSummaryService.Event E(string category, string type, int day)
        {
            return new DiagnosticSummaryService.Event { Category = category, Type = type,
                Utc = new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc) };
        }
        private static int Main()
        {
            try
            {
                var data = new List<DiagnosticSummaryService.Event> {
                    E("UI", "TimeoutException", 3), E("UI", "TimeoutException", 1),
                    E("Background", "IOException", 2), E("UI", "IOException", 2),
                    E("UI", "=HYPERLINK", 4), E("Other", "Invalid", 5), null
                };
                var rows = DiagnosticSummaryService.Aggregate(data);
                Check("three valid groups", rows.Count == 3);
                Check("repeated event count", rows[0].Count == 2 && rows[0].Type == "TimeoutException");
                Check("first timestamp", rows[0].FirstUtc.Day == 1);
                Check("last timestamp", rows[0].LastUtc.Day == 3);
                Check("category separation", rows.Count(x => x.Type == "IOException") == 2);
                Check("invalid types excluded", rows.All(x => DiagnosticSummaryService.SafeType(x.Type)));
                Check("safe type validation", !DiagnosticSummaryService.SafeType("+SUM(1)") && DiagnosticSummaryService.SafeType("Safe_123"));
                Check("empty input", DiagnosticSummaryService.Aggregate(new DiagnosticSummaryService.Event[0]).Count == 0);
                var csv = DiagnosticSummaryService.ExportCsv(rows);
                Check("CSV header", csv.StartsWith("Category,ErrorType,Count,FirstUtc,LastUtc\r\n"));
                Check("CSV UTC timestamps", csv.Contains("2026-01-01T12:00:00Z"));
                Check("CSV no formula payload", !csv.Contains("HYPERLINK"));
                Check("CSV row count", csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length == 4);
                Check("CSV rejects unsafe summary", !DiagnosticSummaryService.ExportCsv(new[] { new DiagnosticSummaryService.Summary { Category = "UI", Type = "=evil", Count = 1 } }).Contains("evil"));
                Console.WriteLine("All " + passed + " executable tests passed.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
    }
}
