using System;
using Compze.Utilities.SystemCE.ThreadingCE.Utilities;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IAwaitableMonitorCE
{
   static readonly TimeSpan DefaultTimeout = CompzeEnvironment.IsNCrunch
                                                ? 45.Seconds() //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
                                                : 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is highly likely that we have an in-memory deadlock.

   public static IAwaitableMonitorCE WithDefaultTimeout() => IMonitorCE.CreateAwaitableWithDefaultTimeout();
   public static IAwaitableMonitorCE WithTimeouts(TimeSpan lockTimeout, TimeSpan? waitTimeout = null) => IMonitorCE.CreateAwaitableWithTimeouts(lockTimeout, waitTimeout ?? lockTimeout);

   TimeSpan LockTimeout { get; }
   TimeSpan WaitTimeout { get; }

   IDisposable TakeReadLock(TimeSpan? timeout = null);
   IDisposable TakeUpdateLock(TimeSpan? timeout = null);

   IDisposable TakeReadLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null);
   IDisposable TakeUpdateLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null);

   IDisposable? TryTakeReadLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null);
   IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, TimeSpan? waitTimeout = null, TimeSpan? lockTimeout = null);

   long ContentionCount { get; }

   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);
}
