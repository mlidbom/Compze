using Compze.Threading.Exceptions;

// ReSharper disable ConvertToPrimaryConstructor

namespace Compze.Threading.Interprocess;

public partial interface IPollingAwaitableMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   private class PollingAwaitableMutexCE : IPollingAwaitableMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      readonly IMutex _mutex;

      public PollingAwaitableMutexCE(string name, bool global, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, PollingInterval? pollingInterval, Action? onAbandonedMutex)
      {
         _mutex = global
                     ? IMutex.Global(name, lockTimeout, onAbandonedMutex)
                     : IMutex.Local(name, lockTimeout, onAbandonedMutex);

         WaitTimeout = waitTimeout ?? WaitTimeout.Default;
         PollingInterval = pollingInterval ?? PollingInterval.Default;
      }

      public LockTimeout LockTimeout => _mutex.LockTimeout;
      public long ContentionCount => _mutex.ContentionCount;
      public WaitTimeout WaitTimeout { get; }
      public PollingInterval PollingInterval { get; }
      public bool IsGlobal => _mutex.IsGlobal;
      public string Name => _mutex.Name;

      public IReadLock TakeReadLock(LockTimeout? timeout = null) => (IReadLock)_mutex.TakeLock(timeout);
      public IUpdateLock TakeUpdateLock(LockTimeout? timeout = null) => (IUpdateLock)_mutex.TakeLock(timeout);

      public IReadLock TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock)(TryTakeLockWhen(condition, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException());

      public IUpdateLock TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock)(TryTakeLockWhen(condition, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException());

      public IReadLock? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout);

      public IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout);

      ILock? TryTakeLockWhen(Func<bool> condition, WaitTimeout? waitTimeout, LockTimeout? lockTimeout)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         while(true)
         {
            ILock takenLock = _mutex.TakeLock(effectiveLockTimeout);
            try
            {
               if(condition()) return takenLock;
            }
            catch
            {
               takenLock.Dispose();
               throw;
            }

            takenLock.Dispose();

            if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
               return null;

            Thread.Sleep(PollingInterval);
         }
      }

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) =>
         ((ILockInternals)_mutex).SetTimeToWaitForStackTrace(timeToWaitForStackTrace);

      public void Dispose() => _mutex.Dispose();
   }
}
