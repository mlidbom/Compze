using System.Runtime.InteropServices;

namespace Compze.Internals.SystemCE.IOCE._private;

/// <summary>
/// A Windows error that one of Windows' own file functions, called directly, failed with: told as the exception .NET's own
/// file methods throw for it, so that a caller catches what it would catch from them.
/// </summary>
static class WindowsFileError
{
   internal const int FileNotFound = 2, PathNotFound = 3, AccessDenied = 5, SharingViolation = 32, AlreadyExists = 183;

   /// <summary>The error the last call to a Windows function failed with.</summary>
   internal static int OfTheLastCall => Marshal.GetLastPInvokeError();

   /// <summary>
   /// The exception for <paramref name="error"/>, with a message that starts with <paramref name="action"/>, which failed,
   /// and tells Windows' own message for it, and <paramref name="whatExplainsIt"/> after that.
   /// </summary>
   internal static Exception ExceptionFor(int error, string action, string whatExplainsIt = "")
   {
      var message = $"{action}: {Marshal.GetPInvokeErrorMessage(error)} (Windows error {error}).{whatExplainsIt}";
      return error switch
      {
         FileNotFound => new FileNotFoundException(message),
         PathNotFound => new DirectoryNotFoundException(message),
         AccessDenied => new UnauthorizedAccessException(message),
         _ => new IOException(message, HResultOf(error))
      };

      static int HResultOf(int windowsError) => unchecked((int)(0x80070000 | (uint)windowsError));
   }
}
