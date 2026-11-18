using Compze.Teventive.MemoryImageStateStore._internal;

namespace Compze.Teventive.MemoryImageStateStore._private.Files;

///<summary>A file's length and last write time, as listing its folder gives them without opening it: what tells whether the file has<br/>
/// changed since they were taken. Equal to another of the same length and last write time.</summary>
sealed class FileStamp : IEquatable<FileStamp>
{
   readonly long _length;
   readonly DateTime _lastWriteTimeUtc;

   internal FileStamp(long length, DateTime lastWriteTimeUtc)
   {
      _length = length;
      _lastWriteTimeUtc = lastWriteTimeUtc;
   }

   internal static FileStamp Of(ListedFile file) => new(file.Length, file.LastWriteTimeUtc);

   public bool Equals(FileStamp? other) => other != null && other._length == _length && other._lastWriteTimeUtc == _lastWriteTimeUtc;

   public override bool Equals(object? obj) => Equals(obj as FileStamp);

   public override int GetHashCode() => HashCode.Combine(_length, _lastWriteTimeUtc);
}
