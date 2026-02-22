using Compze.Utilities.Contracts;
using System;
using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

internal abstract class FileSystemInfoCE : IEquatable<FileSystemInfoCE>
{
   public string AbsolutePath { get; }

   protected abstract FileSystemInfo GetFileSystemInfo();

   protected FileSystemInfoCE(FileSystemInfo info)
   {
      Assert.Argument.Is(info.Exists)
            .Is(Path.IsPathRooted(info.FullName), () => $"{info.FullName} is not an absolute Path. Only absolute paths are supported in order to eliminate the brittleness of an implicit dependency on Environment.CurrentDirectory");
      AbsolutePath = info.FullName;
   }

   public bool Equals(FileSystemInfoCE? other) => other != null
                                               && other.GetType() == GetType()
                                               && other.AbsolutePath == AbsolutePath;

   public override bool Equals(object? obj) => Equals(obj as FileSystemInfoCE);

   public override int GetHashCode() => AbsolutePath.GetHashcodeCE();

   public static bool operator ==(FileSystemInfoCE? left, FileSystemInfoCE? right) => Equals(left, right);
   public static bool operator !=(FileSystemInfoCE? left, FileSystemInfoCE? right) => !Equals(left, right);
}
