using System.IO;
using Compze.Utilities.Contracts;

namespace Compze.Utilities.SystemCE.IOCE;

class FileCE
{
   public readonly FileInfo FileInfo;

   public FileCE(FileInfo fileInfo)
   {
      Assert.Argument.Is(fileInfo.Exists);
      FileInfo = fileInfo;
   }
}
