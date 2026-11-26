using System.Diagnostics;
using Compze.Contracts;
using Compze.Threading.Interprocess._private.ProcessLocks;

namespace Compze.Threading.Interprocess;

public partial interface IProcessLock
{
   private sealed class ProcessLock : IProcessLock
   {
      ///<summary>Who takes the lock, as the description of its holder tells those who wait: this process, by its id and name, and the machine.</summary>
      static readonly string ThisProcess = DescribeThisProcess();

      readonly ProcessLockFile _lockFile;

      internal ProcessLock(string name, DirectoryInfo directory)
      {
         Argument.Assert(IsAFileNameWithNoPathSeparator(name), () => $@"A process lock's name must be a file name, with neither '/' nor '\' in it: '{name}'.");
         Argument.Assert(Directory.Exists(directory.FullName), () => $"The directory of the process lock '{name}' must exist: {directory.FullName}.");
         _lockFile = ProcessLockFile.Named(name, directory);
      }

      public IProcessLockHold? TryTake(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         AssertIsAPurpose(purpose);
         var wait = ProcessLockWait.Begin(waitTimeout, cancellationToken);
         do
         {
            var hold = _lockFile.TryHold(ThisHoldersDescription(purpose));
            if(hold != null) return hold;
         } while(wait.PausedUntilTheNextTry());

         return null;
      }

      public async Task<IProcessLockHold?> TryTakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         AssertIsAPurpose(purpose);
         var wait = ProcessLockWait.Begin(waitTimeout, cancellationToken);
         do
         {
            var hold = _lockFile.TryHold(ThisHoldersDescription(purpose));
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

      ///<summary>What a holder writes beside the lock for those who wait meanwhile: what it holds it for, which process it is, and since when.</summary>
      static string ThisHoldersDescription(string purpose) => $"for {purpose}, by {ThisProcess}, since {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";

      static string DescribeThisProcess()
      {
         using var thisProcess = Process.GetCurrentProcess();
         return $"process {thisProcess.Id} ({thisProcess.ProcessName}) on {Environment.MachineName}";
      }

      static void AssertIsAPurpose(string purpose) => Argument.Assert(!string.IsNullOrWhiteSpace(purpose), () => "Taking a process lock takes a purpose: what the lock is to be held for, to those who wait for it meanwhile.");

      ///<summary>True if <paramref name="name"/> can name a file on the system this runs on, and has neither Windows' path separator nor Unix's in it,<br/>
      /// which would make it a path on one system or the other.</summary>
      static bool IsAFileNameWithNoPathSeparator(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOfAny([..Path.GetInvalidFileNameChars(), '\\', '/']) < 0;
   }
}
