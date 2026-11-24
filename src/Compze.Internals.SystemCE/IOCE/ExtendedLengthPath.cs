using System.Runtime.Versioning;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// A path as Windows' own file functions take it at any length: an extended-length path, the full path after <c>\\?\</c>,
/// or after <c>\\?\UNC\</c> for a share's. For a path handed to Windows directly, as to <c>CreateFileW</c> through
/// platform invoke.
/// </summary>
/// <remarks>
/// Windows' file functions take a path without the prefix only up to 259 characters, unless long paths are enabled for the
/// machine: past that, opening a file fails with "The system cannot find the path specified". .NET's own file methods add
/// the prefix themselves, so a file they wrote at such a path is there, and code that calls Windows directly fails to find
/// it.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class ExtendedLengthPath
{
   //review-naming: the type ExtendedLengthPath.

   /// <summary>
   /// <paramref name="path"/> as an extended-length path: made full first, a relative one against the process's current
   /// directory, as Windows takes a relative path it is given. One that is already an extended-length or device path is
   /// given as it is.
   /// </summary>
   public static string Of(string path)
   {
      var full = Path.GetFullPath(path);
      return full.StartsWithOrdinal(@"\\?\") || full.StartsWithOrdinal(@"\\.\") ? full
             : full.StartsWithOrdinal(@"\\") ? @"\\?\UNC\" + full[2..]
             : @"\\?\" + full;
   }
}
