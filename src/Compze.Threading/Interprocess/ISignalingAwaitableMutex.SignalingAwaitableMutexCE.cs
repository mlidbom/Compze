using Compze.Threading.Exceptions;

namespace Compze.Threading.Interprocess;

public partial interface ISignalingAwaitableMutex
{
#pragma warning disable CS0618 // Type or member is obsolete
   private class SignalingAwaitableMutexCE : ISignalingAwaitableMutex, ILockInternals
#pragma warning restore CS0618 // Type or member is obsolete
   {
      static readonly TimeSpan AbandonedMutexCheckInterval = TimeSpan.FromMilliseconds(50);

      readonly IMutex _mutex;
      readonly InterprocessSignal _signal;

      internal SignalingAwaitableMutexCE(string name, bool global, DirectoryInfo directory, LockTimeout? lockTimeout, WaitTimeout? waitTimeout, Action? onAbandonedMutex)
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

      public IReadLock TakeReadLock(LockTimeout? timeout = null) => (IReadLock)_mutex.TakeLock(timeout);

      public IUpdateLock TakeUpdateLock(LockTimeout? timeout = null)
      {
         var mutexLock = _mutex.TakeLock(timeout);
         return new UpdateLockDisposer(_signal, mutexLock);
      }

      public IReadLock TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: false) ?? throw new AwaitingConditionTimeoutException();

      public IUpdateLock TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: true) ?? throw new AwaitingConditionTimeoutException();

      public IReadLock? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IReadLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: false);

      public IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         (IUpdateLock?)TryTakeLockWhen(condition, waitTimeout, lockTimeout, isUpdate: true);

      IDisposable? TryTakeLockWhen(Func<bool> condition, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, bool isUpdate)
      {
         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var effectiveLockTimeout = lockTimeout ?? LockTimeout;

         var waitStartedAt = DateTime.UtcNow;

         while(true)
         {
            _signal.Snapshot();

            ILock mutexLock = _mutex.TakeLock(effectiveLockTimeout);
            try
            {
               if(condition())
                  return isUpdate ? new UpdateLockDisposer(_signal, mutexLock) : (IReadLock)mutexLock;
            }
            catch
            {
               mutexLock.Dispose();
               throw;
            }

            mutexLock.Dispose();

            if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
               return null;

            while(!_signal.TryAwait(AbandonedMutexCheckInterval))
            {
               if(!effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt))
                  return null;

               _mutex.TryTakeLock(LockTimeout.Zero)?.Dispose();
            }
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
