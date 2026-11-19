namespace Compze.Teventive.MemoryImageStateStore._internal;

///<summary>A file of a class, as listing the class's folder gives it, without opening it: where it is, its length and its last<br/>
/// write time, which together tell whether it has changed since a cache held its snapshot.</summary>
sealed class ListedFile
{
   internal ListedFile(string fullPath, string relativePath, long length, DateTime lastWriteTimeUtc)
   {
      FullPath = fullPath;
      RelativePath = relativePath;
      Length = length;
      LastWriteTimeUtc = lastWriteTimeUtc;
   }

   internal string FullPath { get; }

   ///<summary>The path relative to the class's folder.</summary>
   internal string RelativePath { get; }

   internal long Length { get; }

   internal DateTime LastWriteTimeUtc { get; }
}
