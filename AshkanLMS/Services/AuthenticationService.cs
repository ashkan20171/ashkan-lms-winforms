using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AshkanLMS.Services {
 public sealed class AuthenticationService {
  private static readonly object Gate=new object();
  private readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS");
  private string AccountsPath {get{return Path.Combine(folder,"accounts.dat");}}
  private string LockPath {get{return Path.Combine(folder,"login-locks.dat");}}
  public string SetupMessage {get;private set;}
  private readonly SecurityAuditService audit=new SecurityAuditService();
  public AuthenticationService(){Initialize();}
  private void Initialize(){
   lock(Gate){Directory.CreateDirectory(folder);if(File.Exists(AccountsPath))return;
    byte[] random=new byte[24];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(random);
    string password=Convert.ToBase64String(random).Replace('+','A').Replace('/','B').TrimEnd('=');
    using(var fs=new FileStream(AccountsPath,FileMode.CreateNew,FileAccess.Write,FileShare.None))
    using(var sw=new StreamWriter(fs,Encoding.UTF8))sw.WriteLine(Encode("admin","Administrator",password));
    string setup=Path.Combine(folder,"FIRST_RUN_CREDENTIALS.txt");
    File.WriteAllText(setup,"AshkanLMS initial account\r\nUsername: admin\r\nPassword: "+password+"\r\nDelete this file after signing in.\r\n",Encoding.UTF8);
    SetupMessage="First-run admin credentials saved to: "+setup;
   }
  }
  private static string Encode(string username,string role,string password){
   byte[] salt=new byte[16];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(salt);
   using(var kdf=new Rfc2898DeriveBytes(password,salt,150000,HashAlgorithmName.SHA256))
    return username+"|"+role+"|"+Convert.ToBase64String(salt)+"|"+Convert.ToBase64String(kdf.GetBytes(32));
  }
  private static bool VerifyLine(string line,string password){
   var fields=line.TrimStart('\uFEFF').Split('|');if(fields.Length!=4)return false;
   try{byte[] salt=Convert.FromBase64String(fields[2]),expected=Convert.FromBase64String(fields[3]);
    if(expected.Length!=32||salt.Length!=16)return false;
    using(var kdf=new Rfc2898DeriveBytes(password,salt,150000,HashAlgorithmName.SHA256)){
     byte[] actual=kdf.GetBytes(32);int diff=0;for(int i=0;i<32;i++)diff|=actual[i]^expected[i];return diff==0;
    }
   }catch(FormatException){return false;}
  }
  private static string Key(string username){return Convert.ToBase64String(Encoding.UTF8.GetBytes(username.Trim().ToUpperInvariant()));}
  private Dictionary<string,Tuple<int,long>> ReadLocks(){
   var result=new Dictionary<string,Tuple<int,long>>();if(!File.Exists(LockPath))return result;
   foreach(var line in File.ReadAllLines(LockPath)){var p=line.Split('|');int n;long ticks;
    if(p.Length==3&&int.TryParse(p[1],out n)&&long.TryParse(p[2],out ticks))result[p[0]]=Tuple.Create(n,ticks);
   }return result;
  }
  private void SaveLocks(Dictionary<string,Tuple<int,long>> locks){
   string temp=LockPath+".tmp";File.WriteAllLines(temp,locks.Select(x=>x.Key+"|"+x.Value.Item1+"|"+x.Value.Item2));
   if(File.Exists(LockPath))File.Replace(temp,LockPath,null);else File.Move(temp,LockPath);
  }
  public bool Verify(string username,string password,out string role){
   role="";if(string.IsNullOrWhiteSpace(username)||password==null)return false;
   lock(Gate){string key=Key(username);if(ReadDisabled().Contains(key)){audit.Record(username,"LoginDeniedDisabled",username);return false;}var locks=ReadLocks();Tuple<int,long> state;
    if(locks.TryGetValue(key,out state)&&state.Item1>=5&&DateTime.UtcNow.Ticks<state.Item2){audit.Record(username,"LoginDeniedLocked",username);return false;}
    string match=File.ReadAllLines(AccountsPath).FirstOrDefault(line=>string.Equals(line.TrimStart('\uFEFF').Split('|')[0],username.Trim(),StringComparison.OrdinalIgnoreCase));
    bool valid=match!=null&&VerifyLine(match,password);
    if(valid){locks.Remove(key);SaveLocks(locks);role=match.TrimStart('\uFEFF').Split('|')[1];audit.Record(username,"LoginSucceeded",username);return true;}
    int attempts=state==null||DateTime.UtcNow.Ticks>=state.Item2?1:state.Item1+1;
    locks[key]=Tuple.Create(attempts,DateTime.UtcNow.AddMinutes(attempts>=5?15:5).Ticks);SaveLocks(locks);audit.Record(username,attempts>=5?"AccountLocked":"LoginFailed",username);return false;
   }
  }
  public void ChangePassword(string username,string currentPassword,string newPassword){
   if(string.IsNullOrWhiteSpace(newPassword)||newPassword.Length<12||newPassword.Length>128)throw new ArgumentException("New password must be 12-128 characters.");
   lock(Gate){var lines=File.ReadAllLines(AccountsPath).ToList();int index=lines.FindIndex(x=>string.Equals(x.TrimStart('\uFEFF').Split('|')[0],username,StringComparison.OrdinalIgnoreCase));
    if(index<0||!VerifyLine(lines[index],currentPassword))throw new UnauthorizedAccessException("Current password is incorrect.");
    var fields=lines[index].TrimStart('\uFEFF').Split('|');lines[index]=Encode(fields[0],fields[1],newPassword);
    string temp=AccountsPath+".tmp";File.WriteAllLines(temp,lines,Encoding.UTF8);File.Replace(temp,AccountsPath,AccountsPath+".bak");
    string setup=Path.Combine(folder,"FIRST_RUN_CREDENTIALS.txt");if(File.Exists(setup))File.Delete(setup);audit.Record(username,"PasswordChanged",username);
   }
  }
  public sealed class AccountInfo { public string Username {get;set;} public string Role {get;set;} public bool Disabled {get;set;} }
  private string DisabledPath { get { return Path.Combine(folder,"disabled-accounts.dat"); } }
  private HashSet<string> ReadDisabled(){return new HashSet<string>(File.Exists(DisabledPath)?File.ReadAllLines(DisabledPath):new string[0],StringComparer.Ordinal);}
  private void WriteDisabled(HashSet<string> values){string temp=DisabledPath+".tmp";File.WriteAllLines(temp,values.OrderBy(x=>x).ToArray());if(File.Exists(DisabledPath))File.Replace(temp,DisabledPath,null);else File.Move(temp,DisabledPath);}
  public List<AccountInfo> ListAccounts(string actingRole){
   if(actingRole!="Administrator")throw new UnauthorizedAccessException("Administrator required.");
   lock(Gate){var disabled=ReadDisabled();return File.ReadAllLines(AccountsPath).Select(x=>x.TrimStart('\uFEFF').Split('|')).Where(x=>x.Length==4).Select(x=>new AccountInfo{Username=x[0],Role=x[1],Disabled=disabled.Contains(Key(x[0]))}).ToList();}
  }
  public void SetAccountEnabled(string actingRole,string actingUsername,string username,bool enabled){
   if(actingRole!="Administrator")throw new UnauthorizedAccessException("Administrator required.");
   if(string.Equals(actingUsername,username,StringComparison.OrdinalIgnoreCase)&&!enabled)throw new InvalidOperationException("You cannot disable your own account.");
   lock(Gate){var accounts=ListAccounts(actingRole);if(!accounts.Any(x=>string.Equals(x.Username,username,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Unknown account.");
    if(!enabled&&accounts.Count(x=>x.Role=="Administrator"&&!x.Disabled)<=1&&accounts.Any(x=>x.Role=="Administrator"&&string.Equals(x.Username,username,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("The last active administrator cannot be disabled.");
    var disabled=ReadDisabled();if(enabled)disabled.Remove(Key(username));else disabled.Add(Key(username));WriteDisabled(disabled);audit.Record(actingUsername,enabled?"AccountEnabled":"AccountDisabled",username);
   }
  }
  public void ResetPassword(string actingRole,string username,string newPassword){
   if(actingRole!="Administrator")throw new UnauthorizedAccessException("Administrator required.");
   if(string.IsNullOrWhiteSpace(newPassword)||newPassword.Length<12||newPassword.Length>128)throw new ArgumentException("Password must be 12-128 characters.");
   lock(Gate){var lines=File.ReadAllLines(AccountsPath).ToList();int index=lines.FindIndex(x=>string.Equals(x.TrimStart('\uFEFF').Split('|')[0],username,StringComparison.OrdinalIgnoreCase));
    if(index<0)throw new ArgumentException("Unknown account.");var fields=lines[index].TrimStart('\uFEFF').Split('|');lines[index]=Encode(fields[0],fields[1],newPassword);
    string temp=AccountsPath+".tmp";File.WriteAllLines(temp,lines,Encoding.UTF8);File.Replace(temp,AccountsPath,AccountsPath+".bak");
    var locks=ReadLocks();locks.Remove(Key(username));SaveLocks(locks);audit.Record("Administrator","PasswordReset",username);
   }
  }
  public void CreateAccount(string actingRole,string username,string role,string password){
   if(actingRole!="Administrator")throw new UnauthorizedAccessException("Administrator required.");
   if(string.IsNullOrWhiteSpace(username)||username.Length>64||username.Contains("|")||username.Contains("\n")||username.Contains("\r"))throw new ArgumentException("Invalid username.");
   if(role!="Administrator"&&role!="Instructor"&&role!="Student")throw new ArgumentException("Invalid role.");
   if(string.IsNullOrWhiteSpace(password)||password.Length<12||password.Length>128)throw new ArgumentException("Password must be 12-128 characters.");
   lock(Gate){var lines=File.ReadAllLines(AccountsPath).ToList();if(lines.Any(x=>string.Equals(x.TrimStart('\uFEFF').Split('|')[0],username.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Username already exists.");
    lines.Add(Encode(username.Trim(),role,password));string temp=AccountsPath+".tmp";File.WriteAllLines(temp,lines,Encoding.UTF8);File.Replace(temp,AccountsPath,AccountsPath+".bak");audit.Record("Administrator","AccountCreated",username);
   }
  }
  // A session is bound to the current credential record. Password resets invalidate it.
  public string GetSessionStamp(string username) {
   if(string.IsNullOrWhiteSpace(username))return null;
   lock(Gate) {
    if(ReadDisabled().Contains(Key(username)))return null;
    var line=File.ReadAllLines(AccountsPath).FirstOrDefault(x=>string.Equals(x.TrimStart('\uFEFF').Split('|')[0],username.Trim(),StringComparison.OrdinalIgnoreCase));
    if(line==null)return null;
    var parts=line.TrimStart('\uFEFF').Split('|');
    if(parts.Length!=4)return null;
    using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(line.TrimStart('\uFEFF'))));
   }
  }
  public bool IsAccountEnabled(string username){lock(Gate){return !ReadDisabled().Contains(Key(username));}}
 }
}
