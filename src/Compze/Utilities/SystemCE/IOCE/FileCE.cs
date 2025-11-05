using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

public class FileCE : FileSystemInfoCE
{
   public FileInfo GetFileInfo() => new FileInfo(AbsolutePath);

   public FileCE(FileInfo fileInfo) : base(fileInfo){}

   protected override FileSystemInfo GetFileSystemInfo() => GetFileInfo();
}
