using System;
using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

public partial class DirectoryCE
{

   internal static class StandardDirectories
   {
      static DirectoryCE GetStandardDirectory(Environment.SpecialFolder folder)
      {
         var path = Environment.GetFolderPath(folder);
         var directoryInfo = new DirectoryInfo(path);
         return new DirectoryCE(directoryInfo);
      }

      public static DirectoryCE LocalApplicationData => GetStandardDirectory(Environment.SpecialFolder.LocalApplicationData);
   }
}
