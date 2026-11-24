using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// Puts a file written beside another in that one's place, in one step, so that a reader finds either the old file
/// whole or the new one whole: the way to write anew a file that others may be reading meanwhile.
/// </summary>
/// <remarks>
/// On Windows, <see cref="File.Move(string, string, bool)"/> can do it only while no one has the old file open: Windows
/// refuses to rename over a file that is open, even when every reader lets it be deleted, with "Access to the path is
/// denied".
/// </remarks>
/// <remarks>
/// On Windows this renames with POSIX semantics instead, as Windows has done since Windows 10's version 1709 on NTFS: the
/// old file's name is taken from it at once, and a reader that has it open, letting it be deleted, reads it on until it
/// closes it. A reader that doesn't let it be deleted still refuses the rename, as it refuses a deletion: such readers read
/// with <see cref="FileReadLettingItBeReplaced"/>.<br/>
/// On other operating systems a rename has these semantics already, and this is <see cref="File.Move(string, string, bool)"/>.
/// </remarks>
/// <remarks>review-naming: the type <c>FileReplacement</c>.</remarks>
public static class FileReplacement
{
   /// <summary>
   /// Puts <paramref name="by"/>, a file written beside <paramref name="file"/> and closed, in the place of
   /// <paramref name="file"/>, whether there is one there or not.
   /// </summary>
   /// <exception cref="FileNotFoundException">There is no file <paramref name="by"/>.</exception>
   /// <exception cref="UnauthorizedAccessException">A reader of <paramref name="file"/> doesn't let it be deleted.</exception>
   public static void Replace(string file, string by)
   {
      if(OperatingSystem.IsWindows()) ReplaceWithPosixSemanticsOnWindows(file, by);
      else File.Move(by, file, overwrite: true);
   }

   [SupportedOSPlatform("windows")]
   static void ReplaceWithPosixSemanticsOnWindows(string file, string by)
   {
      using var replacement = NativeMethods.CreateFileW(ExtendedLengthPath.Of(by), DeleteAccess, FileShare.ReadWrite | FileShare.Delete, 0, FileMode.Open, 0, 0);
      if(replacement.IsInvalid) throw LastError($"Opening {by} to put it in the place of {file}");
      var information = RenameInformation(ExtendedLengthPath.Of(file));
      if(!NativeMethods.SetFileInformationByHandle(replacement, FileRenameInfoEx, information, information.Length))
         throw LastError($"Putting {by} in the place of {file}");
   }

   /// <summary>
   /// <c>FILE_RENAME_INFO</c> for <c>FileRenameInfoEx</c>: its flags, to replace a file of the name with POSIX semantics,
   /// no root folder, and the full <paramref name="name"/>, ended by a NUL.
   /// </summary>
   static byte[] RenameInformation(string name)
   {
      var information = new byte[OffsetOfTheName + (name.Length + 1) * sizeof(char)];
      BinaryPrimitives.WriteInt32LittleEndian(information.AsSpan(OffsetOfTheFlags), ReplaceIfExists | PosixSemantics);
      BinaryPrimitives.WriteInt32LittleEndian(information.AsSpan(OffsetOfTheNamesLength), name.Length * sizeof(char));
      Encoding.Unicode.GetBytes(name, information.AsSpan(OffsetOfTheName));
      return information;
   }

   const int OffsetOfTheFlags = 0;

   /// <summary>
   /// Where <c>FILE_RENAME_INFO</c>'s length of the name is: after its flags and its root folder's handle, which is
   /// aligned to the size of a pointer and as long.
   /// </summary>
   static int OffsetOfTheNamesLength => 2 * IntPtr.Size;

   static int OffsetOfTheName => OffsetOfTheNamesLength + sizeof(int);

   const int ReplaceIfExists = 0x1, PosixSemantics = 0x2, FileRenameInfoEx = 22;
   const uint DeleteAccess = 0x10000;
   const int ErrorFileNotFound = 2, ErrorPathNotFound = 3, ErrorAccessDenied = 5;

   /// <summary>The exception .NET's own file methods throw for the Windows error the last call failed with.</summary>
   static Exception LastError(string action)
   {
      var error = Marshal.GetLastPInvokeError();
      var message = $"{action}: {Marshal.GetPInvokeErrorMessage(error)} (Windows error {error}).";
      return error switch
      {
         ErrorFileNotFound => new FileNotFoundException(message),
         ErrorPathNotFound => new DirectoryNotFoundException(message),
         ErrorAccessDenied => new UnauthorizedAccessException(message),
         _ => new IOException(message, HResultOf(error))
      };

      static int HResultOf(int windowsError) => unchecked((int)(0x80070000 | (uint)windowsError));
   }

   static class NativeMethods
   {
      [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern SafeFileHandle CreateFileW(string name, uint access, FileShare sharing, nint security, FileMode mode, uint flags, nint template);

      [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool SetFileInformationByHandle(SafeFileHandle file, int kind, byte[] information, int size);
   }
}
