namespace Compze.Internals.SystemCE.Core.IOCE;

public static class DirectoryInfoCE
{
   extension(DirectoryInfo @this)
   {
      public FileInfo File(string name) => new(Path.Combine(@this.FullName, name));
   }
}
