using System.Collections.Immutable;
using System.Diagnostics;
using Compze.Threading.Exceptions;
using Compze.Threading.Exceptions._private;

namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations.
   // ReSharper disable once ArrangeTypeMemberModifiers A false positive on partial types nested in an interface: without private, MonitorCE would be public, a nested type's default in an interface.
   private partial class MonitorCE : IAwaitableMonitor, IMonitor, ILockInternals
#pragma warning restore CA1001
   {
      public ILock TakeLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TakeLock(_readLock, timeout, cancellationToken);
      public IReadLock TakeReadLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TakeLock(_readLock, timeout, cancellationToken);
      public IUpdateLock TakeUpdateLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => TakeLock(_updateLock, timeout, cancellationToken);

      public IReadLock TakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, _readLock, waitTimeout, lockTimeout, cancellationToken);

      public IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, _updateLock, waitTimeout, lockTimeout, cancellationToken);

      public IReadLock? TryTakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, _readLock, waitTimeout, lockTimeout, cancellationToken);

      public IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, _updateLock, waitTimeout, lockTimeout, cancellationToken);

      public LockTimeout LockTimeout { get; }
      public WaitTimeout WaitTimeout { get; }
      public long ContentionCount => _monitor.ContentionCount;

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) => _stackTraceFetchTimeout = timeToWaitForStackTrace;

      readonly ThinMonitorWrapper _monitor = new();

      readonly ReadLock _readLock;
      readonly UpdateLock _updateLock;

      static readonly WaitTimeout DefaultTimeToWaitForStackTrace = WaitTimeout.Seconds(10);

      WaitTimeout _stackTraceFetchTimeout;

      ImmutableArray<TakeMonitorLockTimeoutException> _timeoutExceptionsOnOtherThreads = [];

      ///<summary>The signal the next update-lock release completes, waking the async condition waits waiting for it. Null while none waits.<br/>
      /// It is created only when an async wait needs one, so a monitor nobody awaits asynchronously pays for nothing but this field.<br/>
      /// Guarded by the lock.</summary>
      TaskCompletionSource? _nextUpdate;

      public MonitorCE(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null)
      {
         LockTimeout = lockTimeout ?? LockTimeout.Default;
         WaitTimeout = waitTimeout ?? WaitTimeout.Default;
         _readLock = new ReadLock(this);
         _updateLock = new UpdateLock(this);
         _stackTraceFetchTimeout = DefaultTimeToWaitForStackTrace;
      }

      THeldLock TakeLock<THeldLock>(THeldLock heldLock, LockTimeout? lockTimeout, CancellationToken cancellationToken) where THeldLock : class
      {
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;
         return _monitor.TryTakeLock(effectiveLockTimeout, cancellationToken)
                   ? heldLock
                   : throw RegisterTimeoutException(effectiveLockTimeout);
      }

      THeldLock TakeLockWhen<THeldLock>(Func<bool> condition, THeldLock heldLock, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, CancellationToken cancellationToken) where THeldLock : class, IDisposable =>
         TryTakeLockWhen(condition, heldLock, waitTimeout, lockTimeout, cancellationToken) ?? throw new AwaitingConditionTimeoutException();

      THeldLock? TryTakeLockWhen<THeldLock>(Func<bool> condition, THeldLock heldLock, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, CancellationToken cancellationToken) where THeldLock : class, IDisposable
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;

         var waitStartedAt = DateTime.UtcNow;

         TakeLock(heldLock, lockTimeout, cancellationToken);
         // When the token is cancelled, PulseAll wakes any thread blocked in Monitor.Wait so it can observe the cancellation.
         var wakeWaitingThreadsOnCancellation = cancellationToken.CanBeCanceled
                                                   ? cancellationToken.Register(_monitor.AcquireLockAndNotifyWaitingThreads)
                                                   : default;
         try
         {
            if(effectiveWaitTimeout.IsInfinite)
            {
               while(!condition())
               {
                  cancellationToken.ThrowIfCancellationRequested();
                  _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout.Infinite);
               }
            } else
            {
               while(!condition())
               {
                  cancellationToken.ThrowIfCancellationRequested();
                  if(effectiveWaitTimeout.IsExpired(waitStartedAt))
                  {
                     heldLock.Dispose();
                     return null;
                  }

                  _monitor.ReleaseLockAndReacquireItOnPulseOrTimeout(effectiveWaitTimeout.TimeRemaining(waitStartedAt));
               }
            }
         }
         catch
         {
            ReleaseLock();
            throw;
         }
         finally
         {
            wakeWaitingThreadsOnCancellation.Unregister(); //Dispose would cause a deadlock in certain situations (test verified) so we use Unregister.
         }

         return heldLock;
      }

      void ReleaseLock()
      {
         SetBlockingThreadsDisposeStackTraceOnRegisteredTimeoutExceptions();
         _monitor.ReleaseLock();
      }

      void ReleaseUpdateLock()
      {
         _monitor.NotifyWaitingThreadsAboutUpdates();
         NotifyAsyncWaitsAboutUpdates();
         ReleaseLock();
      }

      Exception RegisterTimeoutException(LockTimeout timedOutAfter)
      {
         var exception = new TakeMonitorLockTimeoutException(timedOutAfter, _stackTraceFetchTimeout);
         ImmutableInterlocked.Update(ref _timeoutExceptionsOnOtherThreads, static (registered, newlyRegistered) => registered.Add(newlyRegistered), exception);
         return exception;
      }

      void SetBlockingThreadsDisposeStackTraceOnRegisteredTimeoutExceptions()
      {
         if(_timeoutExceptionsOnOtherThreads.IsEmpty) return;

         var stackTrace = new StackTrace(fNeedFileInfo: true);
         foreach(var exception in ImmutableInterlocked.InterlockedExchange(ref _timeoutExceptionsOnOtherThreads, []))
         {
            exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
         }
      }
   }
}
