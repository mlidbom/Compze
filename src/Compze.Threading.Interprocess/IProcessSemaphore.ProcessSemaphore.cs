using Compze.Contracts;
using Compze.Threading.Interprocess._private.ProcessLocks;
using Compze.Threading.Interprocess._private.ProcessSemaphores;

namespace Compze.Threading.Interprocess;

public partial interface IProcessSemaphore
{
   private sealed class ProcessSemaphore : IProcessSemaphore
   {
      const string Kind = "process semaphore";

      readonly ProcessSemaphoreFiles _files;

      internal ProcessSemaphore(string name, int units, DirectoryInfo directory)
      {
         ProcessLockArguments.AssertIsAName(name, Kind);
         ProcessLockArguments.AssertExists(directory, name, Kind);
         Argument.Assert(units >= 1, () => $"A process semaphore has at least 1 unit: '{name}' was to have {units}.");
         _files = new ProcessSemaphoreFiles(name, units, directory);
      }

      public int Units => _files.Units;

      public IProcessSemaphoreHold? TryTake(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         AssertIsATake(units, purpose);
         using var take = ProcessSemaphoreTake.Begin(_files, units, purpose, waitTimeout, cancellationToken);
         while(!take.HoldsAllItsUnitsAfterALook())
            if(!take.WaitedForTheNextLook()) return null;

         return take.Completed();
      }

      public async Task<IProcessSemaphoreHold?> TryTakeAsync(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default)
      {
         AssertIsATake(units, purpose);
         using var take = ProcessSemaphoreTake.Begin(_files, units, purpose, waitTimeout, cancellationToken);
         while(!take.HoldsAllItsUnitsAfterALook())
            if(!await take.WaitedForTheNextLookAsync().ConfigureAwait(false)) return null;

         return take.Completed();
      }

      public IProcessSemaphoreHold Take(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default) =>
         TryTake(units, purpose, waitTimeout, cancellationToken) ?? throw GaveUpWaiting(units, purpose, waitTimeout);

      public async Task<IProcessSemaphoreHold> TakeAsync(int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken = default) =>
         await TryTakeAsync(units, purpose, waitTimeout, cancellationToken).ConfigureAwait(false) ?? throw GaveUpWaiting(units, purpose, waitTimeout);

      public string DescribeHoldersAndWaiters()
      {
         using var queue = _files.OpenTheQueue();
         return queue.Read(_files.DescribeHoldersAndWaiters);
      }

      TimeoutException GaveUpWaiting(int units, string purpose, WaitTimeout waitTimeout) =>
         new($"Gave up after {waitTimeout} waiting to take {WaitingRequest.UnitsTold(units)} of the process semaphore {_files.Name} for {purpose}. {DescribeHoldersAndWaiters()}");

      void AssertIsATake(int units, string purpose)
      {
         Argument.Assert(units >= 1 && units <= Units, () => $"A take of the process semaphore {_files.Name}, which has {WaitingRequest.UnitsTold(Units)}, takes at least 1 unit, and at most {Units}: not {units}.");
         ProcessLockArguments.AssertIsAPurpose(purpose, Kind);
      }
   }
}
