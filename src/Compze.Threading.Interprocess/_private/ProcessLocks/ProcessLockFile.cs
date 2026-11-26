namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>The file an <see cref="IProcessLock"/> is: <c>{name}.lock</c> in the lock's directory. Its holder keeps it open in a way that keeps every<br/>
/// other holder out, and the operating system closes it when the holder's process dies. How a file keeps others out differs between Windows and<br/>
/// the other systems, and with it where the holder writes what it holds the lock for.</summary>
abstract class ProcessLockFile
{
   internal static ProcessLockFile Named(string name, DirectoryInfo directory) =>
      OperatingSystem.IsWindows() ? new WindowsProcessLockFile(name, directory) : new UnixProcessLockFile(name, directory);

   protected ProcessLockFile(string name, DirectoryInfo directory) => LockFilePath = Path.Combine(directory.FullName, $"{name}.lock");

   internal string LockFilePath { get; }

   ///<summary>Opens the file as its holder, and writes <paramref name="holderDescription"/> where those who wait read it: or returns null if another<br/>
   /// holder has it open.</summary>
   internal IProcessLockHold? TryHold(string holderDescription)
   {
      var lockFile = TryOpenAsItsHolder();
      try
      {
         if(lockFile == null) return null;

         WriteTheHolderDescription(lockFile, holderDescription);
         var hold = new ProcessLockFileHold(lockFile);
         lockFile = null; //The hold has it now: what follows must not close it.
         return hold;
      }
      finally
      {
         //Unless the hold has it: a lock file whose holder description couldn't be written must not leave the lock held with no hold to release it.
         lockFile?.Dispose();
      }
   }

   ///<summary>True if a holder has the file open now. It looks by opening the file as a holder would, and closes it at once, writing nothing: a<br/>
   /// holder that tries to open it in that moment finds it held.</summary>
   internal bool IsHeld()
   {
      using var lockFile = TryOpenAsItsHolder();
      return lockFile == null;
   }

   ///<summary>What the last holder wrote of what it holds the lock for: null if no holder has written it yet.</summary>
   internal abstract string? LastHoldersDescription();

   ///<summary>Opens the file in the way that keeps every other holder out: or returns null if another holder has it open so.</summary>
   protected abstract FileStream? TryOpenAsItsHolder();

   ///<summary>Writes, as the holder of <paramref name="lockFile"/>, what it holds the lock for, where those who wait read it.</summary>
   protected abstract void WriteTheHolderDescription(FileStream lockFile, string holderDescription);
}
