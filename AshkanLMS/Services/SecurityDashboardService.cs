using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
namespace AshkanLMS.Services {
 public static class SecurityDashboardService {
  public sealed class Summary { public int Total, FailedLogins, LockedAccounts, ExpiredSessions, RevokedSessions, SuccessfulLogins; }
  public static Summary Aggregate(IEnumerable<SecurityAuditService.Entry> entries) {
   var s=new Summary(); foreach(var e in entries ?? Enumerable.Empty<SecurityAuditService.Entry>()) {
    if(e==null)continue; s.Total++;
    if(e.Event=="LoginFailed")s.FailedLogins++;
    else if(e.Event=="AccountLocked")s.LockedAccounts++;
    else if(e.Event=="SessionIdleTimeout")s.ExpiredSessions++;
    else if(e.Event=="SessionRevoked")s.RevokedSessions++;
    else if(e.Event=="LoginSuccess"||e.Event=="LoginSucceeded")s.SuccessfulLogins++;
   } return s;
  }
  static string Csv(string v) { return "\""+(v??"").Replace("\"","\"\"")+"\""; }
  public static string ExportCsv(IEnumerable<SecurityAuditService.Entry> entries) {
   var b=new StringBuilder("UtcTime,Actor,Event,Subject\r\n");
   foreach(var e in entries ?? Enumerable.Empty<SecurityAuditService.Entry>()) {
    if(e==null)continue;
    b.Append(Csv(e.UtcTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ",CultureInfo.InvariantCulture))).Append(',')
     .Append(Csv(e.Actor)).Append(',').Append(Csv(e.Event)).Append(',').Append(Csv(e.Subject)).Append("\r\n");
   }return b.ToString();
  }
 }
}
