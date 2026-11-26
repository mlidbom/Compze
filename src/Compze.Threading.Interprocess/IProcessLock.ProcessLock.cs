using Compze.Threading.Interprocess._private.ProcessLocks;

namespace Compze.Threading.Interprocess;

public partial interface IProcessLock
{
   private sealed class ProcessLock : IProcessLock
   {
      const string Kind = "process lock";

      readonly ProcessLockFile _lockFile;

      internal ProcessLock(string name, DirectoryInfo directory)
      {
         ProcessLockArguments.AssertIsAName(name, Kind);
         ProcessLockArguments.AssertExists(directory, name, Kind);
         _lockFile = ProcessLockFile.Named(name, directory);
      }

      public IProcessLockHold? TryTake(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         ProcessLockArguments.AssertIsAPurpose(purpose, Kind);
         var wait = ProcessLockWait.Begin(waitTimeout, cancellationToken);
         do
         {
            var hold = _lockFile.TryHold(HoldersDescription.OfThisProcessHoldingItFor(purpose));
            if(hold != null) return hold;
         } while(wait.PausedUntilTheNextTry());

         return null;
      }

      public async Task<IProcessLockHold?> TryTakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         ProcessLockArguments.AssertIsAPurpose(purpose, Kind);
         var wait = ProcessLockWait.Begin(waitTimeout, cancellationToken);
         do
         {
            var hold = _lockFile.TryHold(HoldersDescription.OfThisProcessHoldingItFor(purpose));
            if(hold != null) return hold;
         } while(await wait.PausedUntilTheNextTryAsync().ConfigureAwait(false));

         return null;
      }

      public IProcessLockHold Take(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default) =>
         TryTake(purpose, waitTimeout, cancellationToken) ?? throw GaveUpWaiting(purpose, waitTimeout);

      public async Task<IProcessLockHold> TakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default) =>
         await TryTakeAsync(purpose, waitTimeout, cancellationToken).ConfigureAwait(false) ?? throw GaveUpWaiting(purpose, waitTimeout);

      TimeoutException GaveUpWaiting(string purpose, WaitTimeout waitTimeout) =>
         new($"Gave up after {waitTimeout} waiting to take the process lock {_lockFile.LockFilePath} for {purpose}. It is held {_lockFile.LastHoldersDescription() ?? "by a holder that hasn't yet written what for"}.");
   }
}
