using System;
using AshkanLMS.Core;
namespace AshkanLMS.Services {
 public static class AuthorizationService {
  public static bool CanManageAcademics(string role) { return role=="Administrator" || role=="Instructor"; }
  public static bool CanManageAccounts(string role) { return role=="Administrator"; }
  public static void RequireAcademicWrite() {
   if(string.IsNullOrWhiteSpace(AppSession.UserName) || !CanManageAcademics(AppSession.Role))
    throw new UnauthorizedAccessException("You do not have permission to modify academic records.");
  }
  public static void RequireAccountAdmin() {
   if(string.IsNullOrWhiteSpace(AppSession.UserName) || !CanManageAccounts(AppSession.Role))
    throw new UnauthorizedAccessException("Administrator permission is required.");
  }
 }
}
