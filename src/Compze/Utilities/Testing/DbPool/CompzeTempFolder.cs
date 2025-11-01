using System;
using System.IO;
using Compze.Utilities.Testing.DbPool.SystemCE.ThreadingCE;

namespace Compze.Utilities.Testing.DbPool;

#pragma warning disable IDE0065
using SPath = Path;
#pragma warning restore IDE0065

///<summary>Manages the AppData folder in a machine wide thread safe manner.</summary>
static class CompzeFolder
{
   static readonly MutexCE MachineWideLock = MutexCE.ForMutexNamed(nameof(CompzeFolder));
   static readonly string DefaultPath = SPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Compze");
   static readonly string Path = EnsureFolderExists();

   internal static string EnsureFolderExists(string folderName) => MachineWideLock.ExecuteWithLock(() =>
   {
      var folder = SPath.Combine(Path, folderName);
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