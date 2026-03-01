using System;
using Compze.Utilities.SystemCE.ThreadingCE.Utilities;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitor
{
   static readonly TimeSpan DefaultTimeoutTimeSpan = CompzeEnvironment.IsNCrunch
                                                        ? 45.Seconds() //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
                                                        : 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is highly likely that we have an in-memory deadlock.

   public static IMonitor WithDefaultTimeout() => new MonitorCE(new LockTimeout(DefaultTimeoutTimeSpan), new WaitTimeout(DefaultTimeoutTimeSpan));
   public static IMonitor New(LockTimeout timeout) => new MonitorCE(timeout, new WaitTimeout(timeout.Value));

   internal static IAwaitableMonitor CreateAwaitableWithDefaultTimeout() => new MonitorCE(new LockTimeout(DefaultTimeoutTimeSpan), new WaitTimeout(DefaultTimeoutTimeSpan));
   internal static IAwaitableMonitor CreateAwaitableWithTimeouts(LockTimeout lockTimeout, WaitTimeout waitTimeout) => new MonitorCE(lockTimeout, waitTimeout);

   LockTimeout LockTimeout { get; }

   IDisposable TakeLock(LockTimeout? timeout = null);

   long ContentionCount { get; }

   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);
}
