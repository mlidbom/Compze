namespace Compze.Internals.SystemCE.IOCE;

public static class DirectoryInfoCE
{
   extension(DirectoryInfo @this)
   {
      ///<summary>Returns a <see cref="FileInfo"/> for a file named <paramref name="name"/> in <paramref name="this"/></summary>
      public FileInfo File(string name) => new(Path.Combine(@this.FullName, name));
   }
}
