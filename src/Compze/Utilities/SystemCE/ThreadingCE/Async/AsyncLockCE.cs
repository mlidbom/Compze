using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Functional;
using Compze.Utilities.Misc;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE.ThreadingCE.Async;

public interface IAsyncLockCE : IDisposable
{
   static readonly TimeSpan DefaultTimeout = CompzeEnvironment.IsNCrunch
                                                ? 45.Seconds() //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
                                                : 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is highly likely that we have an in-memory deadlock.

   static IAsyncLockCE WithDefaultTimeout() => new IAsyncLockCE.AsyncLockCE(DefaultTimeout);
   static IAsyncLockCE WithTimeout(TimeSpan timeout) => new IAsyncLockCE.AsyncLockCE(timeout);

   Task<unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);


   class AsyncLockCE : IAsyncLockCE
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      readonly AsyncLocal<int> _lockEntranceCount = new();
      readonly TimeSpan _timeout;

      internal AsyncLockCE(TimeSpan timeout) => _timeout = timeout;

      public async Task<unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.AsFunc()).caf();

      public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
      {
         await using var exit = new AsyncDisposable(Exit).ConfigureAwait(false);//Analyzer does not understand caf() here for some reason and emits a warning.
         if(_lockEntranceCount.Value == 0)
         {
            if(!await _semaphore.WaitAsync(_timeout).caf())
            {
               throw new AsyncLockTimeoutException(_timeout);
            }
         }

         _lockEntranceCount.Value += 1;
         return await lockedAction().caf();
      }

      public unit Locked(Action lockedAction) => Locked(lockedAction.AsFunc());

      public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
      {
         using var exit = new Disposable(Exit);
         if(_lockEntranceCount.Value == 0)
         {
            if(!_semaphore.Wait(_timeout))
            {
               throw new AsyncLockTimeoutException(_timeout);
            }
         }

         _lockEntranceCount.Value += 1;
         return lockedAction();
      }

      void Exit()
      {
         _lockEntranceCount.Value -= 1;
         if(_lockEntranceCount.Value == 0)
         {
            _semaphore.Release();
         }
      }

      public void Dispose() => _semaphore.Dispose();
   }
}
