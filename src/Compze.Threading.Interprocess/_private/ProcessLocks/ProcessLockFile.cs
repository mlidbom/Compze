namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>The file an <see cref="IProcessLock"/> is: its holder keeps it open in a way that keeps every other holder out, and the operating system<br/>
/// closes it when the holder's process dies. How a file keeps others out differs between Windows and the other systems, and with it where the<br/>
/// holder writes what it holds the lock for.</summary>
abstract class ProcessLockFile
{
   internal static ProcessLockFile Named(string name, DirectoryInfo directory)
   {
      var lockFilePath = Path.Combine(directory.FullName, $"{name}.lock");
      return OperatingSystem.IsWindows()
                ? new WindowsProcessLockFile(lockFilePath)
                : new UnixProcessLockFile(lockFilePath, holderDescriptionFilePath: Path.Combine(directory.FullName, $"{name}.holder"));
   }

   protected ProcessLockFile(string lockFilePath) => LockFilePath = lockFilePath;

   internal string LockFilePath { get; }

   ///<summary>Opens the file as its holder, and writes <paramref name="holderDescription"/> where those who wait read it: or returns null if another<br/>
   /// holder has it open.</summary>
   internal abstract IProcessLockHold? TryHold(string holderDescription);

   ///<summary>What the last holder wrote of what it holds the lock for: null if no holder has written it yet.</summary>
   internal abstract string? HolderDescription();
}
