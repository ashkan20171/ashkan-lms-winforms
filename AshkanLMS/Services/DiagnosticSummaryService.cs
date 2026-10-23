using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AshkanLMS.Services
{
    // Pure, UI-independent diagnostic aggregation. Inputs must already be sanitized.
    internal static class DiagnosticSummaryService
    {
        internal sealed class Event
        {
            internal DateTime Utc;
            internal string Category;
            internal string Type;
        }
        internal sealed class Summary
        {
            public string Category { get; set; }
            public string Type { get; set; }
            public int Count { get; set; }
            public DateTime FirstUtc { get; set; }
            public DateTime LastUtc { get; set; }
        }
        internal static List<Summary> Aggregate(IEnumerable<Event> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Where(x => x != null && (x.Category == "UI" || x.Category == "Background") && SafeType(x.Type))
                .GroupBy(x => new { x.Category, x.Type })
                .Select(g => new Summary { Category = g.Key.Category, Type = g.Key.Type, Count = g.Count(),
                    FirstUtc = g.Min(x => x.Utc), LastUtc = g.Max(x => x.Utc) })
                .OrderByDescending(x => x.Count).ThenBy(x => x.Category, StringComparer.Ordinal)
                .ThenBy(x => x.Type, StringComparer.Ordinal).ToList();
        }
        internal static bool SafeType(string type)
        {
            return !string.IsNullOrEmpty(type) && type.Length <= 90 &&
                type.All(ch => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') ||
                    (ch >= '0' && ch <= '9') || ch == '_');
        }
        internal static string ExportCsv(IEnumerable<Summary> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var csv = new StringBuilder("Category,ErrorType,Count,FirstUtc,LastUtc\r\n");
            foreach (var item in source)
            {
                if (item == null || (item.Category != "UI" && item.Category != "Background") || !SafeType(item.Type)) continue;
                csv.Append(Quote(item.Category)).Append(',').Append(Quote(item.Type)).Append(',')
                   .Append(item.Count.ToString(CultureInfo.InvariantCulture)).Append(',')
                   .Append(Quote(item.FirstUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))).Append(',')
                   .Append(Quote(item.LastUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))).Append("\r\n");
            }
            return csv.ToString();
        }
        private static string Quote(string value) { return "\"" + (value ?? "").Replace("\"", "\"\"") + "\""; }
    }
}
