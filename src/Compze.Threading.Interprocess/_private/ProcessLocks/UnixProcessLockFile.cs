using Compze.Contracts;

namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>A process lock's file on Linux, macOS and the other Unix systems. .NET locks a file it opens with <see cref="FileShare.None"/> with<br/>
/// <c>flock</c>, which keeps out every other holder, as each opens it the same way. A <c>flock</c> keeps out only those who also lock the file, and<br/>
/// .NET locks every file it opens, so those who wait couldn't read the lock file: the holder writes what it holds the lock for in a file of its own<br/>
/// beside it, <c>{name}.holder</c>.</summary>
sealed class UnixProcessLockFile : ProcessLockFile
{
   ///<summary>The <see cref="Exception.HResult"/> of .NET's <see cref="IOException"/> for a file that another holder has locked: the errno<br/>
   /// <c>EWOULDBLOCK</c>, which is 11 on Linux, and 35 on macOS and the BSDs.</summary>
   static readonly int WouldBlock = OperatingSystem.IsLinux() ? 11 : 35;

   readonly string _holderDescriptionFilePath;

   internal UnixProcessLockFile(string name, DirectoryInfo directory) : base(name, directory) =>
      _holderDescriptionFilePath = Path.Combine(directory.FullName, $"{name}.holder");

   protected override FileStream? TryOpenAsItsHolder()
   {
      try
      {
         return new FileStream(LockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
      }
      catch(IOException exception) when(exception.HResult == WouldBlock)
      {
         return null;
      }
   }

   ///<remarks>Asserts first that the lock file keeps a second holder out: see <see cref="AssertThatTheFileKeepsASecondHolderOut"/>.</remarks>
   protected override void WriteTheHolderDescription(FileStream lockFile, string holderDescription)
   {
      AssertThatTheFileKeepsASecondHolderOut();
      File.WriteAllText(_holderDescriptionFilePath, holderDescription);
   }

   internal override string? LastHoldersDescription() =>
      File.Exists(_holderDescriptionFilePath) ? File.ReadAllText(_holderDescriptionFilePath) : null;

   ///<summary>Asserts that this holder, having opened the lock file, can't open it again. .NET locks nothing, and says nothing, when its file<br/>
   /// locking is turned off, by the <c>DOTNET_SYSTEM_IO_DISABLEFILELOCKING</c> environment variable or the <c>System.IO.DisableFileLocking</c><br/>
   /// switch, nor where the file system doesn't support <c>flock</c>: the lock file would then keep no other holder out.</summary>
   void AssertThatTheFileKeepsASecondHolderOut()
   {
      using var secondOpening = TryOpenAsItsHolder();
      State.Assert(secondOpening == null, () => $"The process lock file {LockFilePath} keeps no second holder out, so it can't be a lock: .NET's file locking is turned off, by the DOTNET_SYSTEM_IO_DISABLEFILELOCKING environment variable or the System.IO.DisableFileLocking switch, or the file system doesn't support flock, as some network file systems don't.");
   }
}
