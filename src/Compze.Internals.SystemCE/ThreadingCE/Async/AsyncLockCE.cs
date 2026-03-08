using System.Diagnostics;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Internals.SystemCE.ThreadingCE.Async;

public interface IAsyncLockCE : IDisposable
{
   static IAsyncLockCE WithDefaultTimeout() => new AsyncLockCE(LockTimeout.Default);
   static IAsyncLockCE New(LockTimeout timeout) => new AsyncLockCE(timeout);

   Task<unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);

   void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);


   public class AsyncLockCE : IAsyncLockCE
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      readonly AsyncLocal<int> _lockEntranceCount = new();
      readonly LockTimeout _timeout;
      readonly Lock _timeoutLock = new();

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(1);

      WaitTimeout _stackTraceFetchTimeout;
      IReadOnlyList<AsyncLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AsyncLockTimeoutException>();

      internal AsyncLockCE(LockTimeout timeout)
      {
         _timeout = timeout;
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      public async Task<unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.ToAsyncFunc()).caf();

      public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
      {
         if(_lockEntranceCount.Value == 0)
         {
            if(!await _semaphore.WaitAsync(_timeout).caf())
            {
               throw RegisterTimeoutException();
            }
         }

         _lockEntranceCount.Value += 1;
         await using var exit = new AsyncDisposable(Exit).ConfigureAwait(false);//Analyzer does not understand caf() here for some reason and emits a warning.
         return await lockedAction().caf();
      }

      public unit Locked(Action lockedAction) => Locked(lockedAction.ToFunc());

      public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
      {
         if(_lockEntranceCount.Value == 0)
         {
            if(!_semaphore.Wait(_timeout))
            {
               throw RegisterTimeoutException();
            }
         }

         _lockEntranceCount.Value += 1;
         using var exit = new Disposable(Exit);
         return lockedAction();
      }

      void Exit()
      {
         _lockEntranceCount.Value -= 1;
         if(_lockEntranceCount.Value == 0)
         {
            UpdateAnyRegisteredTimeoutExceptions();
            _semaphore.Release();
         }
      }

      Exception RegisterTimeoutException()
      {
         lock(_timeoutLock)
         {
            var exception = new AsyncLockTimeoutException(_timeout, _stackTraceFetchTimeout);
            OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _timeOutExceptionsOnOtherThreads, exception);
            return exception;
         }
      }

      void UpdateAnyRegisteredTimeoutExceptions()
      {
         // ReSharper disable once InconsistentlySynchronizedField
         if(_timeOutExceptionsOnOtherThreads.Count > 0)
         {
            lock(_timeoutLock)
            {
               var stackTrace = new StackTrace(fNeedFileInfo: true);
               foreach(var exception in _timeOutExceptionsOnOtherThreads)
               {
                  exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
               }

               Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, new List<AsyncLockTimeoutException>());
            }
         }
      }

      public void Dispose() => _semaphore.Dispose();
   }
}
