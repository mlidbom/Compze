using System.Diagnostics;
using Compze.Contracts;
using Compze.Threading.Interprocess._private.ProcessLocks;

namespace Compze.Threading.Interprocess;

public partial interface IProcessLock
{
   private sealed class ProcessLock : IProcessLock
   {
      ///<summary>How long a wait sleeps between its tries to take the lock: the longest a waiter takes to notice a release. Fixed, rather than<br/>
      /// growing as the wait grows, which would hand the lock to whoever began waiting last.</summary>
      static readonly TimeSpan TimeBetweenTries = TimeSpan.FromMilliseconds(50);

      ///<summary>Who takes the lock, as the description of its holder tells those who wait: this process, by its id and name, and the machine.</summary>
      static readonly string ThisProcess = DescribeThisProcess();

      readonly ProcessLockFile _lockFile;

      internal ProcessLock(string name, DirectoryInfo directory)
      {
         Argument.Assert(IsAFileNameOnEverySystem(name), () => $"A process lock's name must be a file name on every system, with no path separator in it: '{name}'.");
         Argument.Assert(Directory.Exists(directory.FullName), () => $"The directory of the process lock '{name}' must exist: {directory.FullName}.");
         _lockFile = ProcessLockFile.Named(name, directory);
      }

      public IProcessLockHold? TryTake(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         AssertIsAPurpose(purpose);
         var waitStartedAt = Stopwatch.GetTimestamp();
         while(true)
         {
            cancellationToken.ThrowIfCancellationRequested();
            if(_lockFile.TryHold(HolderDescription(purpose)) is {} hold) return hold;
            if(TimeBeforeTheNextTry(waitTimeout, waitStartedAt) is not {} pause) return null;

            //Waits on the cancellation token's handle rather than sleeping, so cancellation wakes the wait at once.
            cancellationToken.WaitHandle.WaitOne(pause);
         }
      }

      public async Task<IProcessLockHold?> TryTakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         AssertIsAPurpose(purpose);
         var waitStartedAt = Stopwatch.GetTimestamp();
         while(true)
         {
            cancellationToken.ThrowIfCancellationRequested();
            if(_lockFile.TryHold(HolderDescription(purpose)) is {} hold) return hold;
            if(TimeBeforeTheNextTry(waitTimeout, waitStartedAt) is not {} pause) return null;

            //Suppressed so that cancellation surfaces as the OperationCanceledException thrown above, as in the synchronous wait, not as Task.Delay's TaskCanceledException.
            await Task.Delay(pause, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
         }
      }

      public IProcessLockHold Take(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default) =>
         TryTake(purpose, waitTimeout, cancellationToken) ?? throw GaveUpWaiting(purpose, waitTimeout);

      public async Task<IProcessLockHold> TakeAsync(string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default) =>
         await TryTakeAsync(purpose, waitTimeout, cancellationToken).ConfigureAwait(false) ?? throw GaveUpWaiting(purpose, waitTimeout);

      ///<summary>How long to sleep before trying again: <see cref="TimeBetweenTries"/>, but never past the end of the wait, so that a wait that gives<br/>
      /// up has tried once more at its end. Null once the wait has ended.</summary>
      static TimeSpan? TimeBeforeTheNextTry(WaitTimeout waitTimeout, long waitStartedAt)
      {
         if(waitTimeout.IsInfinite) return TimeBetweenTries;

         var timeLeft = waitTimeout.Value - Stopwatch.GetElapsedTime(waitStartedAt);
         if(timeLeft <= TimeSpan.Zero) return null;
         return timeLeft < TimeBetweenTries ? timeLeft : TimeBetweenTries;
      }

      TimeoutException GaveUpWaiting(string purpose, WaitTimeout waitTimeout) =>
         new($"Gave up after {waitTimeout} waiting to take the process lock {_lockFile.LockFilePath} for {purpose}. It is held {_lockFile.HolderDescription() ?? "by a holder that hasn't yet written what for"}.");

      ///<summary>What a holder writes beside the lock for those who wait meanwhile: what it holds it for, which process it is, and since when.</summary>
      static string HolderDescription(string purpose) => $"for {purpose}, by {ThisProcess}, since {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";

      static string DescribeThisProcess()
      {
         using var thisProcess = Process.GetCurrentProcess();
         return $"process {thisProcess.Id} ({thisProcess.ProcessName}) on {Environment.MachineName}";
      }

      static void AssertIsAPurpose(string purpose) => Argument.Assert(!string.IsNullOrWhiteSpace(purpose), () => "Taking a process lock takes a purpose: what the lock is to be held for, to those who wait for it meanwhile.");

      ///<summary>True if <paramref name="name"/> can name a file on Windows and on Unix alike: neither system's path separator is in it, nor any<br/>
      /// character the system it runs on forbids in a file name.</summary>
      static bool IsAFileNameOnEverySystem(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOfAny([..Path.GetInvalidFileNameChars(), '\\', '/']) < 0;
   }
}
