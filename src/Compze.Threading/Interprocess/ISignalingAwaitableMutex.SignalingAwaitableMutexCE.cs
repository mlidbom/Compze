using Compze.Threading.Exceptions;

namespace Compze.Threading.Interprocess;

public partial interface ISignalingAwaitableMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   class SignalingAwaitableMutexCE : ISignalingAwaitableMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      readonly IMutex _mutex;
      readonly InterprocessChangeCounter _changeCounter;

      public SignalingAwaitableMutexCE(string name, bool global, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, PollingInterval? pollingInterval, Action? onAbandonedMutex)
      {
         _mutex = global
            ? IMutex.GlobalNamed(name, lockTimeout, onAbandonedMutex)
            : IMutex.LocalNamed(name, lockTimeout, onAbandonedMutex);

         _changeCounter = new InterprocessChangeCounter(name, global);

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

      public IDisposable TakeReadLock(LockTimeout? timeout = null) => _mutex.TakeLock(timeout);

      public IDisposable TakeUpdateLock(LockTimeout? timeout = null)
      {
         var mutexLock = _mutex.TakeLock(timeout);
         return new UpdateLockDisposer(_changeCounter, mutexLock);
      }

      public IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeReadLockWhen(condition, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      public IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeUpdateLockWhen(condition, waitTimeout, lockTimeout) ?? throw new AwaitingConditionTimeoutException();

      public IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: false);

      IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout, LockTimeout? lockTimeout) =>
         TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: true);

      IDisposable? TryTakeLockWhen(Func<bool> condition, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, bool isUpdate)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         while(true)
         {
            var baseline = _changeCounter.Count;

            IDisposable mutexLock = _mutex.TakeLock(effectiveLockTimeout);
            try
            {
               if(condition())
                  return isUpdate ? new UpdateLockDisposer(_changeCounter, mutexLock) : mutexLock;
            }
            catch
            {
               mutexLock.Dispose();
               throw;
            }

            mutexLock.Dispose();

            if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
               return null;

            while(_changeCounter.Count == baseline)
            {
               if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
                  return null;

               Thread.Sleep(PollingInterval);
            }
         }
      }

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) =>
         ((ILockInternals)_mutex).SetTimeToWaitForStackTrace(timeToWaitForStackTrace);

      public void Dispose()
      {
         _changeCounter.Dispose();
         _mutex.Dispose();
      }

      class UpdateLockDisposer(InterprocessChangeCounter changeCounter, IDisposable mutexLock) : IDisposable
      {
         public void Dispose()
         {
            changeCounter.Increment();
            mutexLock.Dispose();
         }
      }
   }
}
