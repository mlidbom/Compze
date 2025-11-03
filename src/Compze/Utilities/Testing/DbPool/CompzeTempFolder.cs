using System;
using System.IO;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

namespace Compze.Utilities.Testing.DbPool;

///<summary>Manages the AppData folder in a machine wide thread safe manner.</summary>
static class CompzeFolder
{
   static readonly MutexCE MachineWideLock = MutexCE.ForMutexNamed(nameof(CompzeFolder));
   static readonly string DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Compze");
   static readonly string FolderPath = EnsureFolderExists();

   internal static string EnsureFolderExists(string folderName) => MachineWideLock.ExecuteWithLock(() =>
   {
      var folder = Path.Combine(FolderPath, folderName);
      if(!Directory.Exists(folder))
      {
         Directory.CreateDirectory(folder);
      }

      return folder;
   });

   static string EnsureFolderExists()
   {
      return MachineWideLock.ExecuteWithLock(() =>
      {
         if(!Directory.Exists(DefaultPath))
         {
            Directory.CreateDirectory(DefaultPath);
         }
         return DefaultPath;
      });
   }
}