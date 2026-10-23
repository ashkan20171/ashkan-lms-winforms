using System;
using AshkanLMS.Services;
class Program {
 static int passed,failed;
 static void Check(string label,bool ok){if(ok){passed++;Console.WriteLine("PASS "+label);}else{failed++;Console.WriteLine("FAIL "+label);}}
 static int Main(){
  var start=new DateTime(2026,1,1,10,0,0,DateTimeKind.Utc);
  var policy=new SessionLifetimeService(start,TimeSpan.FromMinutes(30));
  Check("new session valid",!policy.IsExpired(start));
  Check("before boundary valid",!policy.IsExpired(start.AddMinutes(29).AddSeconds(59)));
  Check("boundary expires",policy.IsExpired(start.AddMinutes(30)));
  Check("remaining zero when expired",policy.Remaining(start.AddHours(1))==TimeSpan.Zero);
  policy.RecordActivity(start.AddMinutes(20));
  Check("activity extends session",!policy.IsExpired(start.AddMinutes(40)));
  Check("new boundary expires",policy.IsExpired(start.AddMinutes(50)));
  policy.RecordActivity(start.AddMinutes(10));
  Check("stale event cannot rewind clock",policy.IsExpired(start.AddMinutes(50)));
  Check("remaining exact",policy.Remaining(start.AddMinutes(49))==TimeSpan.FromMinutes(1));
  try{new SessionLifetimeService(start,TimeSpan.Zero);Check("zero limit rejected",false);}catch(ArgumentOutOfRangeException){Check("zero limit rejected",true);}
  Console.WriteLine(passed+" passed; "+failed+" failed");return failed==0?0:1;
 }
}
