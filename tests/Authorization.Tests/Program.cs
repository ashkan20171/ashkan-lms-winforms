using System;
using AshkanLMS.Core;
using AshkanLMS.Services;

namespace AshkanLMS.AuthorizationTests {
 internal static class Program {
  private static int passed, failed;
  private static void Check(string name, bool actual, bool expected) {
   if (actual == expected) { Console.WriteLine("PASS " + name); passed++; }
   else { Console.Error.WriteLine("FAIL " + name); failed++; }
  }
  private static void Demand(string name, Action action, bool allowed) {
   try { action(); Check(name, true, allowed); }
   catch (UnauthorizedAccessException) { Check(name, false, allowed); }
   catch (Exception e) { Console.Error.WriteLine("FAIL " + name + " unexpected " + e.GetType().Name); failed++; }
  }
  private static int Main() {
   try {
    foreach (string role in new[] { "Administrator", "Instructor", "Student", "", "administrator", "Unknown" }) {
     Check("Academic role " + role, AuthorizationService.CanManageAcademics(role), role == "Administrator" || role == "Instructor");
     Check("Account role " + role, AuthorizationService.CanManageAccounts(role), role == "Administrator");
     AppSession.UserName = "test-user"; AppSession.Role = role;
     Demand("Academic permission " + role, AuthorizationService.RequireAcademicWrite, role == "Administrator" || role == "Instructor");
     Demand("Account permission " + role, AuthorizationService.RequireAccountAdmin, role == "Administrator");
    }
    AppSession.UserName = ""; AppSession.Role = "Administrator";
    Demand("No identity academic", AuthorizationService.RequireAcademicWrite, false);
    Demand("No identity accounts", AuthorizationService.RequireAccountAdmin, false);
    AppSession.UserName = "   ";
    Demand("Whitespace identity academic", AuthorizationService.RequireAcademicWrite, false);
    Demand("Whitespace identity accounts", AuthorizationService.RequireAccountAdmin, false);
   } finally { AppSession.ClearIdentity(); }
   Console.WriteLine("RBAC checks: " + passed + " passed, " + failed + " failed");
   return failed == 0 ? 0 : 1;
  }
 }
}
