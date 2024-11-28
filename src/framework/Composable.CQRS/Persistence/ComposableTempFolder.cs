using System;
using System.IO;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence;

#pragma warning disable IDE0065
using SPath = Path;
#pragma warning restore IDE0065

///<summary>Manages the Temp folder in a machine wide thread safe manner.</summary>
static class ComposableTempFolder
{
   static readonly MachineWideSingleThreaded MachineWideLock = MachineWideSingleThreaded.For(nameof(ComposableTempFolder));
   static readonly string DefaultPath = SPath.Combine(SPath.GetTempPath(), "Composable_TEMP");
   static readonly string Path = EnsureFolderExists();

   internal static string EnsureFolderExists(string folderName) => MachineWideLock.Execute(() =>
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
      return MachineWideLock.Execute(() =>
      {
         if(!Directory.Exists(DefaultPath))
         {
            Directory.CreateDirectory(DefaultPath);
         }
         return DefaultPath;
      });
   }
}