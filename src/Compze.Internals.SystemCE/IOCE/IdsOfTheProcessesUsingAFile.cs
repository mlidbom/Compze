using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Compze.Internals.SystemCE.IOCE._private;
using Microsoft.Win32.SafeHandles;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// The ids of the processes Windows counts as using a file or folder: each that has it open, a folder that is its current
/// directory included, and each that has it mapped into its memory, as a running program has its program's file, and a
/// process a library it loaded. What tells who holds a file that Windows refuses to open, rename or delete.
/// </summary>
/// <remarks>
/// It tells nothing of what is in a folder, only of the folder itself.<br/>
/// It asks the file system about the one file, through a handle opened only to read the file's attributes, which no other
/// process's sharing refuses, and which Windows doesn't count. That took a processor about 80 ms a file, where searching
/// every handle on the machine took about 700 ms, as did Windows' Restart Manager, which names the processes using files
/// from the same count, and refuses a folder (measured 2026-11-24).
/// </remarks>
[SupportedOSPlatform("windows")]
public static class IdsOfTheProcessesUsingAFile
{
   //review-naming: the type IdsOfTheProcessesUsingAFile.

   /// <summary>
   /// Those of <paramref name="path"/>, a file or folder: none when it is gone. A link is asked about as itself, not what it
   /// links to.
   /// </summary>
   /// <exception cref="UnauthorizedAccessException">The process may not read the attributes of <paramref name="path"/>.</exception>
   /// <exception cref="IOException">Windows didn't tell which processes use it.</exception>
   public static IReadOnlyCollection<int> Of(string path)
   {
      using var file = NativeMethods.CreateFileW(ExtendedLengthPath.Of(path), ReadAttributes, FileShare.ReadWrite | FileShare.Delete, 0, FileMode.Open, BackupSemantics | OpenReparsePoint, 0);
      if(file.IsInvalid)
      {
         var error = WindowsFileError.OfTheLastCall;
         return error is WindowsFileError.FileNotFound or WindowsFileError.PathNotFound ? [] : throw WindowsFileError.ExceptionFor(error, $"Opening {path} to ask who uses it");
      }

      return IdsOfTheProcessesUsing(file, path);
   }

   /// <summary>
   /// Who uses <paramref name="path"/>, each process by the name of its program, such as
   /// <c>Windows counts as using it: MsMpEng (PID 4711)</c>: what a failure that Windows refused the file needs to be
   /// understood. When Windows doesn't tell, why not.
   /// </summary>
   internal static string Described(string path)
   {
      IReadOnlyCollection<int> ids;
      try
      {
         ids = Of(path);
      }
      catch(Exception notTold) when(notTold is IOException or UnauthorizedAccessException) //Told in the failure it explains, which it must not hide.
      {
         return $"Windows didn't tell who uses it: {notTold.Message}";
      }

      if(ids.Count == 0) return "Windows counts no process as using it now";
      var names = NamesOfThePrograms(ids);
      return "Windows counts as using it: " + string.Join(", ", ids.Select(id => $"{names.GetValueOrDefault(id, "a process that has exited since")} (PID {id})"));
   }

   /// <summary>The name of the program each process of <paramref name="ids"/> runs, of those that still run.</summary>
   static Dictionary<int, string> NamesOfThePrograms(IReadOnlyCollection<int> ids)
   {
      var running = Process.GetProcesses();
      try
      {
         return running.Where(it => ids.Contains(it.Id)).ToDictionary(it => it.Id, it => it.ProcessName);
      }
      finally
      {
         foreach(var process in running) process.Dispose();
      }
   }

   static IReadOnlyCollection<int> IdsOfTheProcessesUsing(SafeFileHandle file, string path)
   {
      for(var length = 4096;; length *= 2)
      {
         var buffer = Marshal.AllocHGlobal(length);
         try
         {
            var status = NativeMethods.NtQueryInformationFile(file, out _, buffer, length, FileProcessIdsUsingFileInformation);
            if(status == InfoLengthMismatch) continue;
            if(status != 0) throw new IOException($"Windows didn't tell which processes use {path}: NTSTATUS 0x{status:X8}.");
            var count = Marshal.ReadInt32(buffer);
            return [.. Enumerable.Range(0, count).Select(index => (int)Marshal.ReadIntPtr(buffer, ProcessIdsStart + index * IntPtr.Size))];
         }
         finally
         {
            Marshal.FreeHGlobal(buffer);
         }
      }
   }

   /// <summary>
   /// Where <c>FILE_PROCESS_IDS_USING_FILE_INFORMATION</c> lists the ids, each a <c>ULONG_PTR</c>, as long as a pointer: after
   /// the count, a <c>ULONG</c>, and the padding that aligns the list to the size of a pointer.
   /// </summary>
   static int ProcessIdsStart => IntPtr.Size;

   const int FileProcessIdsUsingFileInformation = 47;
   const uint InfoLengthMismatch = 0xC0000004, ReadAttributes = 0x80, BackupSemantics = 0x02000000, OpenReparsePoint = 0x00200000;

   static class NativeMethods
   {
      [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern SafeFileHandle CreateFileW(string name, uint access, FileShare sharing, nint security, FileMode mode, uint flags, nint template);

      [DllImport("ntdll.dll", ExactSpelling = true)]
      [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
      internal static extern uint NtQueryInformationFile(SafeFileHandle file, out IoStatusBlock status, nint information, int length, int informationClass);
   }

   [StructLayout(LayoutKind.Sequential)]
   struct IoStatusBlock
   {
      public nint Status, Information;
   }
}
