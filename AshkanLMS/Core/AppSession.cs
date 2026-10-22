namespace AshkanLMS.Core {
 public static class AppSession {
  public static string UserName="";
  public static string Role="";
  public static bool Persian=true;
  public static string CredentialStamp="";
  public static void ClearIdentity(){UserName="";Role="";CredentialStamp="";}
 }
}
