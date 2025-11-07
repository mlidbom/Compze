using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE.ThreadingCE.Async;

public class AsyncLockCE
{
   readonly SemaphoreSlim _semaphore = new(1, 1);
   readonly AsyncLocal<int> _lockEntranceCount = new();

   public async Task<unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.AsFunc()).caf();

   public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
   {
      await using var _exit = new AsyncDisposable(Exit);
      if(_lockEntranceCount.Value == 0)
         await _semaphore.WaitAsync();

      _lockEntranceCount.Value += 1;
      return await lockedAction();
   }

   public unit Locked(Action lockedAction) => Locked(lockedAction.AsFunc());

   public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
   {
      using var _exit = new Disposable(Exit);
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
}
