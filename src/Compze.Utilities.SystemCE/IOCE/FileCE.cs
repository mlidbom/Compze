using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

class FileCE : FileSystemInfoCE
{
#pragma warning disable CA1024 // Use properties. No, because that would imply that it is part of the instance state and that changing properties in it would change instance state.
   internal FileInfo GetFileInfo() => new(AbsolutePath);
#pragma warning restore CA1024

   internal FileCE(FileInfo fileInfo) : base(fileInfo){}
}
