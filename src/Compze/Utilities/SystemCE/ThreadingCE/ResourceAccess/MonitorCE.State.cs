using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   public static MonitorCE WithDefaultTimeout() => new(DefaultTimeout);
   public static MonitorCE WithTimeout(TimeSpan timeout) => new(timeout);

   readonly ThinMonitorWrapper _coreLock = new();

   //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.
   readonly IDisposable _readLock;
   readonly IDisposable _updateLock;

#if NCRUNCH
        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(45); //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
#else
   static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is all but guaranteed that we have an in-memory deadlock.
#endif

   public TimeSpan Timeout { get; }
   TimeSpan? _stackTraceFetchTimeout;

   internal MonitorCE(TimeSpan timeout)
   {
      _readLock = new ReadLock(this);
      _stackTraceFetchTimeout = null;
      _updateLock = new UpdateLock(this);
      Timeout = timeout;
   }
}
