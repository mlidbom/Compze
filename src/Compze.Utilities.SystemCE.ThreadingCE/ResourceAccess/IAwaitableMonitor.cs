using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IAwaitableMonitor
{
   public static IAwaitableMonitor WithDefaultTimeout() => IMonitor.CreateAwaitableWithDefaultTimeout();
   public static IAwaitableMonitor WithTimeouts(LockTimeout lockTimeout, WaitTimeout? waitTimeout = null) => IMonitor.CreateAwaitableWithTimeouts(lockTimeout, waitTimeout ?? new WaitTimeout(lockTimeout.Value));

   LockTimeout LockTimeout { get; }
   WaitTimeout WaitTimeout { get; }

   IDisposable TakeReadLock(LockTimeout? timeout = null);
   IDisposable TakeUpdateLock(LockTimeout? timeout = null);

   IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   long ContentionCount { get; }

   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);
}
