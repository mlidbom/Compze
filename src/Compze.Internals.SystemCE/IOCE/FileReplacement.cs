using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Compze.Internals.SystemCE.IOCE._private;
using Microsoft.Win32.SafeHandles;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// Puts a file written beside another in that one's place, in one step, so that a reader finds either the old file
/// whole or the new one whole: the way to write anew a file that others may be reading meanwhile, and to write a file once,
/// such as a claim that the first of several writers makes.
/// </summary>
/// <remarks>
/// On Windows, <see cref="File.Move(string, string, bool)"/> can do it only while no one has the old file open: Windows
/// refuses to rename over a file that is open, even when every reader lets it be deleted, with "Access to the path is
/// denied". And elsewhere, where it doesn't replace a file, it looks for one first and then renames, so that two writers
/// at once may both find none, and the second replaces the first's file.
/// </remarks>
/// <remarks>
/// On Windows this renames with POSIX semantics instead, as Windows has done since Windows 10's version 1709 on NTFS: the
/// old file's name is taken from it at once, and a reader that has it open, letting it be deleted, reads it on until it
/// closes it. A reader that doesn't let it be deleted still refuses the rename, as it refuses a deletion: such readers open
/// the file sharing <see cref="FileShare.Delete"/>. When Windows refuses a file because another has it open, the failure
/// says who Windows counts as using each file involved, as <see cref="IdsOfTheProcessesUsingAFile"/> tells it.<br/>
/// On other operating systems a rename has these semantics already: <see cref="Replace"/> is
/// <see cref="File.Move(string, string, bool)"/>, and <see cref="PutInPlaceUnlessThereIsOne"/> links the file in place,
/// which fails when there is a file there, and then deletes its first name.
/// </remarks>
public static class FileReplacement
{
   //review-naming: the type FileReplacement.

   /// <summary>
   /// Puts <paramref name="by"/>, a file written beside <paramref name="file"/> and closed, in the place of
   /// <paramref name="file"/>, whether there is one there or not.
   /// </summary>
   /// <exception cref="FileNotFoundException">There is no file <paramref name="by"/>.</exception>
   /// <exception cref="IOException">On Windows, a reader of <paramref name="file"/> doesn't let it be deleted.</exception>
   public static void Replace(string file, string by)
   {
      if(OperatingSystem.IsWindows()) PutInPlaceWithPosixSemanticsOnWindows(file, by, AFileAlreadyThere.IsReplaced);
      else File.Move(by, file, overwrite: true);
   }

   /// <summary>
   /// Puts <paramref name="by"/> in the place of <paramref name="file"/> as <see cref="Replace"/> does, unless there is a
   /// file there: false then, leaving both files as they are. Of several writers that put their files in the place at once,
   /// one finds no file there.
   /// </summary>
   /// <exception cref="FileNotFoundException">There is no file <paramref name="by"/>.</exception>
   /// <exception cref="IOException">
   /// On an operating system other than Windows, the file system can't link a file under a second name, as FAT can't.
   /// </exception>
   public static bool PutInPlaceUnlessThereIsOne(string file, string by)
   {
      //review-naming: the method PutInPlaceUnlessThereIsOne.
      return OperatingSystem.IsWindows()
                ? PutInPlaceWithPosixSemanticsOnWindows(file, by, AFileAlreadyThere.IsLeft)
                : LinkInPlaceUnlessThereIsOneElsewhere(file, by);
   }

   /// <summary>What putting a file in a place does with a file that is already there.</summary>
   enum AFileAlreadyThere { IsReplaced, IsLeft }

   /// <summary>
   /// Puts <paramref name="by"/> in the place of <paramref name="file"/>, doing with a file there what
   /// <paramref name="alreadyThere"/> says: false when it left one there.
   /// </summary>
   [SupportedOSPlatform("windows")]
   static bool PutInPlaceWithPosixSemanticsOnWindows(string file, string by, AFileAlreadyThere alreadyThere)
   {
      var failure = FailureToRename();
      if(failure is null) return true;
      if(alreadyThere == AFileAlreadyThere.IsLeft && failure.Error == WindowsFileError.AlreadyExists) return false;
      //Told once the replacement is closed, so that Windows doesn't count this process among those that use it.
      throw failure.Exception();

      FailedCall? FailureToRename()
      {
         using var replacement = NativeMethods.CreateFileW(ExtendedLengthPath.Of(by), DeleteAccess, FileShare.ReadWrite | FileShare.Delete, 0, FileMode.Open, 0, 0);
         if(replacement.IsInvalid) return new FailedCall($"Opening {by} to put it in the place of {file}", by);
         var information = RenameInformation(ExtendedLengthPath.Of(file), alreadyThere);
         return NativeMethods.SetFileInformationByHandle(replacement, FileRenameInfoEx, information, information.Length)
                   ? null
                   : new FailedCall($"Putting {by} in the place of {file}", file, by);
      }
   }

   /// <summary>
   /// <c>FILE_RENAME_INFO</c> for <c>FileRenameInfoEx</c>: its flags, to rename with POSIX semantics, replacing a file of the
   /// name if <paramref name="alreadyThere"/> says so, no root folder, and the full <paramref name="name"/>, ended by a NUL.
   /// </summary>
   static byte[] RenameInformation(string name, AFileAlreadyThere alreadyThere)
   {
      var information = new byte[OffsetOfTheName + (name.Length + 1) * sizeof(char)];
      var flags = alreadyThere == AFileAlreadyThere.IsReplaced ? ReplaceIfExists | PosixSemantics : PosixSemantics;
      BinaryPrimitives.WriteInt32LittleEndian(information.AsSpan(OffsetOfTheFlags), flags);
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

   /// <summary>
   /// A call to one of Windows' file functions that just failed, with the error it failed with, which is read as this is
   /// created: what it was to do, and the files involved.
   /// </summary>
   [SupportedOSPlatform("windows")]
   sealed class FailedCall
   {
      readonly string _action;
      readonly string[] _filesInvolved;

      internal FailedCall(string action, params string[] filesInvolved)
      {
         Error = WindowsFileError.OfTheLastCall;
         _action = action;
         _filesInvolved = filesInvolved;
      }

      internal int Error { get; }

      /// <summary>
      /// The exception that tells the failure. When Windows refused a file because another has it open, with who Windows
      /// counts as using each file involved, as it tells it now: which process held it, perhaps a moment only, is what such
      /// a failure needs to be understood.
      /// </summary>
      internal Exception Exception()
      {
         var whoUsesThem = Error is WindowsFileError.SharingViolation or WindowsFileError.AccessDenied
                              ? string.Concat(_filesInvolved.Select(it => $" {it}: {IdsOfTheProcessesUsingAFile.Described(it)}."))
                              : "";
         return WindowsFileError.ExceptionFor(Error, _action, whoUsesThem);
      }
   }

   /// <summary>
   /// Links <paramref name="by"/> in the place of <paramref name="file"/>, which fails when there is a file there, and then
   /// deletes its first name: false when there was a file there.
   /// </summary>
   [UnsupportedOSPlatform("windows")]
   static bool LinkInPlaceUnlessThereIsOneElsewhere(string file, string by)
   {
      if(NativeMethods.link(NulTerminatedUtf8Of(Path.GetFullPath(by)), NulTerminatedUtf8Of(Path.GetFullPath(file))) == 0)
      {
         File.Delete(by);
         return true;
      }

      var error = Marshal.GetLastPInvokeError();
      if(error == ErrnoFileExists) return false;
      var message = $"Linking {by} in the place of {file}: {Marshal.GetPInvokeErrorMessage(error)} (errno {error}).";
      throw error != ErrnoNoSuchFileOrFolder ? new IOException(message)
            : File.Exists(by) ? new DirectoryNotFoundException(message)
            : new FileNotFoundException(message);
   }

   /// <summary><paramref name="path"/> as POSIX's functions take it: its UTF-8 bytes, ended by a NUL.</summary>
   static byte[] NulTerminatedUtf8Of(string path) => Encoding.UTF8.GetBytes(path + '\0');

   /// <summary><c>errno</c>'s values, which Linux, macOS and the BSDs share.</summary>
   const int ErrnoNoSuchFileOrFolder = 2, ErrnoFileExists = 17;

   static class NativeMethods
   {
      [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern SafeFileHandle CreateFileW(string name, uint access, FileShare sharing, nint security, FileMode mode, uint flags, nint template);

      [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool SetFileInformationByHandle(SafeFileHandle file, int kind, byte[] information, int size);

      /// <summary>
      /// POSIX's <c>link</c>: gives the file <paramref name="existing"/> the second name <paramref name="added"/>, unless a
      /// file has that name. Each path is its UTF-8 bytes, ended by a NUL.
      /// </summary>
      [DllImport("libc", ExactSpelling = true, SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
      internal static extern int link(byte[] existing, byte[] added);
   }
}
