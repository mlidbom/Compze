using Compze.Threading.Exceptions;

namespace Compze.Threading.Interprocess;

public partial interface IAwaitableMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   private class AwaitableMutex : IAwaitableMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      static readonly TimeSpan AbandonedMutexCheckInterval = TimeSpan.FromMilliseconds(50);

      readonly IMutex _mutex;
      readonly InterprocessSignal _signal;

      internal AwaitableMutex(string name, bool global, DirectoryInfo directory, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, Action? onAbandonedMutex)
      {
         _signal = new InterprocessSignal(name, directory);

         // Wrap the user's callback so abandoned-mutex detection also raises the signal,
         // waking any thread stuck waiting for a signal.
         Action wrappedOnAbandonedMutex = () =>
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
         (IReadLock?)TryTakeLockWhen(condition, cancellationToken, waitTimeout, lockTimeout, isUpdate: false) ?? throw new AwaitingConditionTimeoutException();

      public IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, cancellationToken, waitTimeout, lockTimeout, isUpdate: true) ?? throw new AwaitingConditionTimeoutException();

      public IReadLock? TryTakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock?)TryTakeLockWhen(condition, cancellationToken, waitTimeout, lockTimeout, isUpdate: false);

      public IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, cancellationToken, waitTimeout, lockTimeout, isUpdate: true);

      IDisposable? TryTakeLockWhen(Func<bool> condition, CancellationToken cancellationToken, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, bool isUpdate)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         // Take the lock (reentrant if caller already holds it)
         ILock mutexLock = _mutex.TakeLock(cancellationToken, effectiveLockTimeout);
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

                  while(!_signal.TryAwait(AbandonedMutexCheckInterval, ref baseline, cancellationToken))
                  {
                     if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
                     {
                        _mutex.ReacquireToNestingDepth(depth - 1, effectiveLockTimeout);
                        return null;
                     }

                     // Probe the mutex to detect abandoned-mutex scenarios (triggers AbandonedMutexException → callback raises signal)
                     _mutex.TryTakeLock(LockTimeout.Zero)?.Dispose();
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
