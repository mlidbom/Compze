using Compze.Threading.Exceptions;

namespace Compze.Threading.Interprocess;

public partial interface ISignalingAwaitableMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   class SignalingAwaitableMutexCE : ISignalingAwaitableMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      static readonly PollingInterval CounterPollingInterval = PollingInterval.Milliseconds(1);
      static readonly TimeSpan SafetyCheckInterval = TimeSpan.FromMilliseconds(50);

      readonly IMutex _mutex;
      readonly InterprocessChangeCounter _changeCounter;

      internal SignalingAwaitableMutexCE(string name, bool global, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, Action? onAbandonedMutex)
      {
         _changeCounter = new InterprocessChangeCounter(name, global);

         // Wrap the user's callback so abandoned-mutex detection also increments the counter,
         // waking any thread stuck in the counter-polling loop.
         Action wrappedOnAbandonedMutex = () =>
         {
            _changeCounter.Increment();
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

      public IDisposable TakeLock(LockTimeout? timeout = null) => _mutex.TakeLock(timeout);

      public IDisposable? TryTakeLock(LockTimeout? timeout = null) => _mutex.TryTakeLock(timeout);

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

            var abandonedMutexCheckInterval = DateTime.UtcNow + SafetyCheckInterval;

            while(_changeCounter.Count == baseline)
            {
               if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
                  return null;

               if(DateTime.UtcNow >= abandonedMutexCheckInterval)
               {
                  TryTakeMutexLockToDetectAbandonedMutexExceptionsTriggeringCounterIncrement();
                  abandonedMutexCheckInterval = DateTime.UtcNow + SafetyCheckInterval;
               }

               Thread.Sleep(CounterPollingInterval);
            }
         }
      }

      void TryTakeMutexLockToDetectAbandonedMutexExceptionsTriggeringCounterIncrement() => _mutex.TryTakeLock(LockTimeout.Zero)?.Dispose();

      public void SetTimeToWaitForStackTrace(WaitTimeout timeToWaitForStackTrace) =>
         ((ILockInternals)_mutex).SetTimeToWaitForStackTrace(timeToWaitForStackTrace);

      public void Dispose()
      {
         _changeCounter.Dispose();
         _mutex.Dispose();
      }

      class UpdateLockDisposer(InterprocessChangeCounter changeCounter, IDisposable mutexLock) : IDisposable
      {
         readonly InterprocessChangeCounter _changeCounter = changeCounter;
         readonly IDisposable _mutexLock = mutexLock;

         public void Dispose()
         {
            _changeCounter.Increment();
            _mutexLock.Dispose();
         }
      }
   }
}
