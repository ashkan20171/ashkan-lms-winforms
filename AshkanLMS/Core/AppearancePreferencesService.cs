using System;
using System.IO;
namespace AshkanLMS.Core {
 public static class AppearancePreferencesService {
  static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AshkanLMS","appearance.txt"); } }
  public static bool LoadDarkMode() {
   try { return File.Exists(FilePath) && string.Equals(File.ReadAllText(FilePath).Trim(),"dark",StringComparison.OrdinalIgnoreCase); }
   catch(IOException) { return false; } catch(UnauthorizedAccessException) { return false; }
  }
  public static void SaveDarkMode(bool dark) {
   Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
   File.WriteAllText(FilePath,dark?"dark":"light");
  }
 }
}
