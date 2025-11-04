using Compze.Utilities.Contracts;
using System;
using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

public abstract class FileSystemInfoCE : IEquatable<FileSystemInfoCE>
{
   readonly string _path;

   protected FileSystemInfoCE(FileSystemInfo fileSystemInfo)
   {
      Assert.Argument.Is(fileSystemInfo.Exists);
      _path = fileSystemInfo.FullName;
   }

   public bool Equals(FileSystemInfoCE? other) => other != null
                                               && other.GetType() == GetType()
                                               && other._path == _path;

   public override bool Equals(object? obj) => Equals(obj as FileSystemInfoCE);

   public override int GetHashCode() => _path.GetHashcodeOrdinal();

   public static bool operator ==(FileSystemInfoCE? left, FileSystemInfoCE? right) => Equals(left, right);
   public static bool operator !=(FileSystemInfoCE? left, FileSystemInfoCE? right) => !Equals(left, right);
}
