using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Compze.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE.ThreadingCE.Async;

public interface IAsyncLockCE : IDisposable
{
   static readonly LockTimeout DefaultLockTimeout = new(CompzeEnvironment.IsNCrunch
                                                           ? 45.Seconds() //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
                                                           : 2.Minutes()); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is highly likely that we have an in-memory deadlock.

   static IAsyncLockCE WithDefaultTimeout() => new AsyncLockCE(DefaultLockTimeout);
   static IAsyncLockCE New(LockTimeout timeout) => new AsyncLockCE(timeout);

   Task<unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);

   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);


   public class AsyncLockCE : IAsyncLockCE
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      readonly AsyncLocal<int> _lockEntranceCount = new();
      readonly LockTimeout _timeout;
      readonly Lock _timeoutLock = new();

      static readonly TimeSpan DefaultTimeToWaitForStackTrace = 1.Seconds();

      TimeSpan _stackTraceFetchTimeout;
      IReadOnlyList<AsyncLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AsyncLockTimeoutException>();

      public AsyncLockCE(LockTimeout timeout)
      {
         _timeout = timeout;
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      public void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      public async Task<unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.AsFunc()).caf();

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

      public unit Locked(Action lockedAction) => Locked(lockedAction.AsFunc());

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
