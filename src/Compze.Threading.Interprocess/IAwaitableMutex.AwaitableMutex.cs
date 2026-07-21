using Compze.Threading.Exceptions;
using Compze.Threading.Interprocess._internal;

namespace Compze.Threading.Interprocess;

public partial interface IAwaitableMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   private class AwaitableMutex : IAwaitableMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      ///<summary>Bounds each <see cref="InterprocessSignal.TryAwait(TimeSpan, ref long, DateTime, CancellationToken)"/> chunk while awaiting a condition:
      /// whenever a chunk times out without a signal, the waiter probes the mutex to detect abandonment.<br/>
      /// The interval is thus both the worst-case abandoned-mutex detection latency and a once-per-interval wakeup floor on the
      /// power cost of a long wait — keep it long; abandonment is a rare crash scenario.</summary>
      static readonly TimeSpan AbandonedMutexCheckInterval = TimeSpan.FromSeconds(1);

      readonly IMutex _mutex;
      readonly InterprocessSignal _signal;

      internal AwaitableMutex(string name, bool global, DirectoryInfo directory, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, ISignalPollingPolicy? signalPollingPolicy, Action? onAbandonedMutex)
      {
         _signal = new InterprocessSignal(name, directory, signalPollingPolicy);

         // Wrap the user's callback so abandoned-mutex detection also raises the signal,
         // waking any thread stuck waiting for a signal.
         var wrappedOnAbandonedMutex = () =>
         {
            _signal.Raise();
            onAbandonedMutex?.Invoke();
         };

         _mutex = global
                     ? IMutex.Global(name, lockTimeout, wrappedOnAbandonedMutex)
                     : IMutex.Local(name, lockTimeout, wrappedOnAbandonedMutex);

         WaitTimeout = waitTimeout ?? WaitTimeout.Default;
      }

      public LockTimeout LockTimeout => _mutex.LockTimeout;
      public long ContentionCount => _mutex.ContentionCount;
      public WaitTimeout WaitTimeout { get; }
      public bool IsGlobal => _mutex.IsGlobal;
      public string Name => _mutex.Name;

      public IReadLock TakeReadLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null) => (IReadLock)_mutex.TakeLock(cancellationToken, timeout);

      public IUpdateLock TakeUpdateLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null)
      {
         var mutexLock = _mutex.TakeLock(cancellationToken, timeout);
         return new UpdateLockDisposer(_signal, mutexLock);
      }

      public IReadLock TakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: false, cancellationToken) ?? throw new AwaitingConditionTimeoutException();

      public IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: true, cancellationToken) ?? throw new AwaitingConditionTimeoutException();

      public IReadLock? TryTakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: false, cancellationToken);

      public IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: true, cancellationToken);

      IDisposable? TryTakeLockWhen(Func<bool> condition, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, bool isUpdate, CancellationToken cancellationToken)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         // Take the lock (reentrant if caller already holds it)
         var mutexLock = _mutex.TakeLock(cancellationToken, effectiveLockTimeout);
         try
         {
            while(!condition())
            {
               cancellationToken.ThrowIfCancellationRequested();

               // Snapshot the signal baseline while holding the lock — any signal raised after this point will be visible to TryAwait.
               var baseline = _signal.Snapshot();

               // Release ALL nesting levels (including outer locks held by the caller),
               // analogous to Monitor.Wait() which releases all recursive lock levels.
               var depth = _mutex.ReleaseAllNestingLevels();

               try
               {
                  if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
                  {
                     // Timeout — restore caller's nesting state (our lock level is excluded) and return null
                     _mutex.ReacquireToNestingDepth(depth - 1, effectiveLockTimeout);
                     return null;
                  }

                  while(!_signal.TryAwait(NextAwaitChunkTimeout(), ref baseline, waitStartedAt, cancellationToken))
                  {
                     if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
                     {
                        _mutex.ReacquireToNestingDepth(depth - 1, effectiveLockTimeout);
                        return null;
                     }

                     // Probe the mutex to detect abandoned-mutex scenarios (triggers AbandonedMutexException → callback raises signal)
                     _mutex.TryTakeLock(LockTimeout.Zero, cancellationToken)?.Dispose();
                  }
               }
               catch
               {
                  // Mirror Monitor.Wait() semantics: always reacquire the lock before letting exceptions propagate.
                  // This ensures callers' using blocks can dispose locks without hitting "mutex not owned" errors.
                  _mutex.ReacquireToNestingDepth(depth, effectiveLockTimeout);
                  throw;
               }

               // Signal received — reacquire to full depth (including our level) and re-check condition
               _mutex.ReacquireToNestingDepth(depth, effectiveLockTimeout);
            }
         }
         catch
         {
            mutexLock.Dispose();
            throw;
         }

         // Condition is true while holding the lock
         return isUpdate ? new UpdateLockDisposer(_signal, mutexLock) : (IReadLock)mutexLock;

         // Clamp each chunk to the remaining wait time so a finite wait timeout is honored precisely instead of overshooting by up to a whole AbandonedMutexCheckInterval.
         TimeSpan NextAwaitChunkTimeout()
         {
            if(effectiveWaitTimeout.IsInfinite) return AbandonedMutexCheckInterval;
            TimeSpan remainingWaitTime = effectiveWaitTimeout.TimeRemaining(waitStartedAt);
            return remainingWaitTime < AbandonedMutexCheckInterval ? remainingWaitTime : AbandonedMutexCheckInterval;
         }
      }

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) =>
         ((ILockInternals)_mutex).SetTimeToWaitForStackTrace(timeToWaitForStackTrace);

      public void Dispose()
      {
         _signal.Dispose();
         _mutex.Dispose();
      }

      class UpdateLockDisposer(InterprocessSignal signal, IDisposable mutexLock) : IUpdateLock
      {
#pragma warning disable CA2213
         readonly InterprocessSignal _signal = signal;
#pragma warning restore CA2213
         readonly IDisposable _mutexLock = mutexLock;

         public void Dispose()
         {
            _signal.Raise();
            _mutexLock.Dispose();
         }
      }
   }
}
