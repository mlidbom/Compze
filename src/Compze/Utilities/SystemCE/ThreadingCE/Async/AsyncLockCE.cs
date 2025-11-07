using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE.ThreadingCE.Async;

public interface IAsyncLockCE : IDisposable
{
   static IAsyncLockCE WithDefaultTimeout() => new AsyncLockCE();
   Task<unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);


   public class AsyncLockCE : IAsyncLockCE
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      readonly AsyncLocal<int> _lockEntranceCount = new();

      public async Task<unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.AsFunc()).caf();

      public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
      {
         await using var exit = new AsyncDisposable(Exit).ConfigureAwait(false);//Analyzer does not understand caf() here for some reason and emits a warning.
         if(_lockEntranceCount.Value == 0)
            await _semaphore.WaitAsync().caf();

         _lockEntranceCount.Value += 1;
         return await lockedAction().caf();
      }

      public unit Locked(Action lockedAction) => Locked(lockedAction.AsFunc());

      public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
      {
         using var exit = new Disposable(Exit);
         if(_lockEntranceCount.Value == 0)
            _semaphore.Wait();

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
