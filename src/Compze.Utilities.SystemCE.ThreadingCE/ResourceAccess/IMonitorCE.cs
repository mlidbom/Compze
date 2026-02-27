using System;
using Compze.Utilities.SystemCE.ThreadingCE.Utilities;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitorCE
{
   static readonly TimeSpan DefaultTimeout = CompzeEnvironment.IsNCrunch
                                                ? 45.Seconds() //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
                                                : 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is highly likely that we have an in-memory deadlock.

   public static IMonitorCE WithDefaultTimeout() => new MonitorCE(DefaultTimeout, DefaultTimeout);
   public static IMonitorCE WithTimeout(TimeSpan timeout) => new MonitorCE(timeout, timeout);

   internal static IAwaitableMonitorCE CreateAwaitableWithDefaultTimeout() => new MonitorCE(DefaultTimeout, DefaultTimeout);
   internal static IAwaitableMonitorCE CreateAwaitableWithTimeouts(TimeSpan lockTimeout, TimeSpan waitTimeout) => new MonitorCE(lockTimeout, waitTimeout);

   TimeSpan LockTimeout { get; }

   IDisposable TakeLock(TimeSpan? timeout = null);

   long ContentionCount { get; }

   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);
}
