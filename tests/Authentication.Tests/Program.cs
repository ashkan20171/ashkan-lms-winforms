using System;
using System.IO;
using AshkanLMS.Services;
namespace AshkanLMS.AuthenticationTests {
 internal static class Program {
  static int passed, failed;
  static void Check(string name,bool condition) {if(condition){passed++;Console.WriteLine("PASS "+name);}else{failed++;Console.Error.WriteLine("FAIL "+name);}}
  static void Denied(string name,Action action) {try{action();Check(name,false);}catch(UnauthorizedAccessException){Check(name,true);}}
  static void Invalid(string name,Action action) {try{action();Check(name,false);}catch(ArgumentException){Check(name,true);}}
  static int Main(){
   string folder=Path.Combine(Path.GetTempPath(),"AshkanLMS-AuthTests-"+Guid.NewGuid().ToString("N"));
   try {
    var auth=new AuthenticationService(folder);
    Check("bootstrap accounts created",File.Exists(Path.Combine(folder,"accounts.dat")));
    string setup=File.ReadAllText(Path.Combine(folder,"FIRST_RUN_CREDENTIALS.txt"));
    string initial=setup.Split(new[]{"Password: "},StringSplitOptions.None)[1].Split('\r','\n')[0];
    string role;
    Check("initial admin login",auth.Verify("admin",initial,out role)&&role=="Administrator");
    Check("wrong password denied",!auth.Verify("admin","wrong",out role));
    auth.CreateAccount("Administrator","teacher","Instructor","TeacherPassword123!");
    Check("instructor login",auth.Verify("teacher","TeacherPassword123!",out role)&&role=="Instructor");
    Denied("non-admin cannot create",()=>auth.CreateAccount("Student","x","Student","Password12345!"));
    Invalid("short password rejected",()=>auth.CreateAccount("Administrator","weak","Student","short"));
    Check("accounts listed",auth.ListAccounts("Administrator").Count==2);
    Denied("non-admin cannot list",()=>auth.ListAccounts("Instructor"));
    auth.ChangePassword("teacher","TeacherPassword123!","NewTeacherPassword123!");
    Check("old password rejected",!auth.Verify("teacher","TeacherPassword123!",out role));
    Check("new password accepted",auth.Verify("teacher","NewTeacherPassword123!",out role));
    string stamp=auth.GetSessionStamp("teacher");
    auth.ResetPassword("Administrator","teacher","ResetTeacherPassword123!");
    Check("reset invalidates session stamp",stamp!=auth.GetSessionStamp("teacher"));
    Check("reset credential accepted",auth.Verify("teacher","ResetTeacherPassword123!",out role));
    auth.SetAccountEnabled("Administrator","admin","teacher",false);
    Check("disabled account denied",!auth.Verify("teacher","ResetTeacherPassword123!",out role));
    Check("disabled session invalid",auth.GetSessionStamp("teacher")==null);
    auth.SetAccountEnabled("Administrator","admin","teacher",true);
    Check("re-enabled login",auth.Verify("teacher","ResetTeacherPassword123!",out role));
    try{auth.SetAccountEnabled("Administrator","admin","admin",false);Check("self-disable prevented",false);}catch(InvalidOperationException){Check("self-disable prevented",true);}
    for(int i=0;i<5;i++)auth.Verify("teacher","incorrect",out role);
    Check("locked account denies correct password",!auth.Verify("teacher","ResetTeacherPassword123!",out role));
    auth.ResetPassword("Administrator","teacher","AfterLockPassword123!");
    Check("admin reset clears lock",auth.Verify("teacher","AfterLockPassword123!",out role));
    Check("security log in isolated directory",File.Exists(Path.Combine(folder,"security-events.log")));
   } catch(Exception ex){failed++;Console.Error.WriteLine(ex);}
   finally {try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
   Console.WriteLine("Authentication: "+passed+" passed, "+failed+" failed");return failed==0?0:1;
  }
 }
}