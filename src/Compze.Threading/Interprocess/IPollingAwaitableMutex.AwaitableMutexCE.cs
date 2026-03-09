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
            ? IMutex.GlobalNamed(name, lockTimeout, onAbandonedMutex)
            : IMutex.LocalNamed(name, lockTimeout, onAbandonedMutex);

         WaitTimeout = waitTimeout ?? WaitTimeout.Default;
         PollingInterval = pollingInterval ?? PollingInterval.Default;
      }

      public LockTimeout LockTimeout => _mutex.LockTimeout;
      public long ContentionCount => _mutex.ContentionCount;
      public WaitTimeout WaitTimeout { get; }
      public PollingInterval PollingInterval { get; }
      public bool IsGlobal => _mutex.IsGlobal;
      public string Name => _mutex.Name;

      public IDisposable TakeLock(LockTimeout? timeout = null) => _mutex.TakeLock(timeout);

      public IDisposable TakeReadLock(LockTimeout? timeout = null) => TakeLock(timeout);
      public IDisposable TakeUpdateLock(LockTimeout? timeout = null) => TakeLock(timeout);

      public IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      public IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      public IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TakeLockWhen(condition, waitTimeout, lockTimeout);

      IDisposable? TakeLockWhen(Func<bool> condition, WaitTimeout? waitTimeout, LockTimeout? lockTimeout)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         while(true)
         {
            IDisposable takenLock = TakeLock(effectiveLockTimeout);
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
