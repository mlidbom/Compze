namespace Compze.Internals.SystemCE.IOCE;

public class FileCE : FileSystemInfoCE
{
#pragma warning disable CA1024 // Use properties. No, because that would imply that it is part of the instance state and that changing properties in it would change instance state.
   public FileInfo GetFileInfo() => new(AbsolutePath);
#pragma warning restore CA1024

   internal FileCE(FileInfo fileInfo) : base(fileInfo){}
}
