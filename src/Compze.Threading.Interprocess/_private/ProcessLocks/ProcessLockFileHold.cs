namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>A held <see cref="IProcessLock"/>: its lock file, held open. Disposing it closes the file, which releases the lock, from whichever thread<br/>
/// disposes it, and only the first time.</summary>
sealed class ProcessLockFileHold : IProcessLockHold
{
   FileStream? _lockFileHeldOpen;

   internal ProcessLockFileHold(FileStream lockFileHeldOpen) => _lockFileHeldOpen = lockFileHeldOpen;

   public void Dispose() => Interlocked.Exchange(ref _lockFileHeldOpen, null)?.Dispose();
}
