using System.Diagnostics;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;
using Compze.Threading;
using Compze.Internals.SystemCE._private;

namespace Compze.Internals.SystemCE.ThreadingCE.Async;

///<summary>A lock that may be held across awaits, which synchronous and asynchronous code take alike. It is re-entrant per async<br/>
/// flow, so a nested use on the same flow doesn't deadlock, while independent flows take turns. Taking it times out after its<br/>
/// <see cref="LockTimeout"/>.</summary>
///<remarks>It isn't disposable: nothing it holds needs disposing, and disposing a lock that is held, or waited for, would break<br/>
/// its holder's release of it and its waiters.</remarks>
///<remarks>Work started while the lock is held, such as a task, inherits the holder's flow. It may enter while the hold lasts,<br/>
/// and the lock stays held until it leaves too. Once the hold has ended, that work is an independent flow, and takes its turn.</remarks>
public interface IAsyncLockCE
{
   static IAsyncLockCE WithDefaultTimeout() => new AsyncLockCE(LockTimeout.Default);
   static IAsyncLockCE New(LockTimeout timeout) => new AsyncLockCE(timeout);

   Task<Unit> LockedAsync(Func<Task> lockedAction);
   Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction);
   Unit Locked(Action lockedAction);
   TReturn Locked<TReturn>(Func<TReturn> lockedAction);

   void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace);


#pragma warning disable CA1001 // The semaphore holds nothing to dispose: SemaphoreSlim allocates only its AvailableWaitHandle, which this never asks for. See IAsyncLockCE's remarks.
   public class AsyncLockCE : IAsyncLockCE
#pragma warning restore CA1001
   {
      readonly SemaphoreSlim _semaphore = new(1, 1);
      ///<summary>The hold this flow took, or inherited from the flow it was started on. A flow carries the hold rather than a<br/>
      /// count of entrances: work started while the lock was held inherits it, and once the hold has ended it lets no one in.</summary>
      readonly AsyncLocal<Hold?> _holdOfThisFlow = new();
      readonly LockTimeout _timeout;
      readonly Lock _timeoutLock = new();

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(10);

      WaitTimeout _stackTraceFetchTimeout;
      IReadOnlyList<AsyncLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AsyncLockTimeoutException>();

      internal AsyncLockCE(LockTimeout timeout)
      {
         _timeout = timeout;
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      public async Task<Unit> LockedAsync(Func<Task> lockedAction) => await LockedAsync(lockedAction.ToAsyncFunc()).caf();

      public async Task<TReturn> LockedAsync<TReturn>(Func<Task<TReturn>> lockedAction)
      {
         var hold = _holdOfThisFlow.Value;
         if(hold?.TryEnterAgain() != true)
         {
            if(!await _semaphore.WaitAsync(_timeout).caf())
            {
               throw RegisterTimeoutException();
            }

            hold = new Hold();
            _holdOfThisFlow.Value = hold; //An async method's changes to its flow don't reach its caller's.
         }

         await using var exit = new AsyncDisposable(() => Exit(hold)).ConfigureAwait(false);//Analyzer does not understand caf() here for some reason and emits a warning.
         return await lockedAction().caf();
      }

      public Unit Locked(Action lockedAction) => Locked(lockedAction.ToFunc());

      public TReturn Locked<TReturn>(Func<TReturn> lockedAction)
      {
         var holdBefore = _holdOfThisFlow.Value;
         var hold = holdBefore;
         if(hold?.TryEnterAgain() != true)
         {
            if(!_semaphore.Wait(_timeout))
            {
               throw RegisterTimeoutException();
            }

            hold = new Hold();
            _holdOfThisFlow.Value = hold;
         }

         try
         {
            return lockedAction();
         }
         finally
         {
            Exit(hold);
            _holdOfThisFlow.Value = holdBefore; //A synchronous method changes its caller's flow: this leaves it as it was.
         }
      }

      void Exit(Hold hold)
      {
         if(hold.Leave())
         {
            UpdateAnyRegisteredTimeoutExceptions();
            _semaphore.Release();
         }
      }

      ///<summary>One holding of the lock, from the flow that took it until the last of the flows that entered it again has left.</summary>
      sealed class Hold
      {
         int _entrances = 1;

         ///<summary>Enters the hold again, unless it has ended. An ended hold stays ended.</summary>
         internal bool TryEnterAgain()
         {
            var entrances = Volatile.Read(ref _entrances);
            while(entrances > 0)
            {
               var found = Interlocked.CompareExchange(ref _entrances, entrances + 1, entrances);
               if(found == entrances) return true;
               entrances = found;
            }

            return false;
         }

         ///<summary>Leaves the hold, returning whether that ended it.</summary>
         internal bool Leave() => Interlocked.Decrement(ref _entrances) == 0;
      }

      Exception RegisterTimeoutException()
      {
         //refactor: _timeoutLock is a raw lock, here and in UpdateAnyRegisteredTimeoutExceptions, where the locking standard asks for
         //   Compze.Threading's primitives, such as IMonitor, which this package already builds on.
         lock(_timeoutLock)
         {
            var exception = new AsyncLockTimeoutException(_timeout, _stackTraceFetchTimeout);
            Interlocked.Exchange(ref _timeOutExceptionsOnOtherThreads, [.._timeOutExceptionsOnOtherThreads, exception]);
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
   }
}
