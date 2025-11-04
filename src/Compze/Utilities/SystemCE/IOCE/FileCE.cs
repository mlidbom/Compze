using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

public class FileCE : FileSystemInfoCE
{
   public readonly FileInfo FileInfo;

   public FileCE(FileInfo fileInfo) : base(fileInfo) => FileInfo = fileInfo;

   public void Delete() => File.Delete(FileInfo.FullName);
}
