using System;
namespace AshkanLMS.Services {
 // Pure, clock-injected idle policy: no UI or persistence dependency.
 public sealed class SessionLifetimeService {
  public static readonly TimeSpan DefaultIdleLimit=TimeSpan.FromMinutes(30);
  private readonly TimeSpan idleLimit;
  private DateTime lastActivityUtc;
  public SessionLifetimeService(DateTime nowUtc,TimeSpan limit){
   if(limit<=TimeSpan.Zero)throw new ArgumentOutOfRangeException("limit");
   idleLimit=limit;lastActivityUtc=nowUtc.ToUniversalTime();
  }
  public void RecordActivity(DateTime nowUtc){
   DateTime utc=nowUtc.ToUniversalTime();
   if(utc>lastActivityUtc)lastActivityUtc=utc;
  }
  public bool IsExpired(DateTime nowUtc){return nowUtc.ToUniversalTime()-lastActivityUtc>=idleLimit;}
  public TimeSpan Remaining(DateTime nowUtc){
   TimeSpan remaining=idleLimit-(nowUtc.ToUniversalTime()-lastActivityUtc);
   return remaining>TimeSpan.Zero?remaining:TimeSpan.Zero;
  }
 }
}
