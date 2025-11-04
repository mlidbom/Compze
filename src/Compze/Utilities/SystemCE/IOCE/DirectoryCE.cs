
using System.IO;
using Compze.Utilities.Contracts;

namespace Compze.Utilities.SystemCE.IOCE;

class DirectoryCE
{
   public readonly DirectoryInfo DirectoryInfo;

   public DirectoryCE(DirectoryInfo directoryInfo)
   {
      DirectoryInfo = directoryInfo;
      Assert.Argument.Is(DirectoryInfo.Exists);
   }
}
