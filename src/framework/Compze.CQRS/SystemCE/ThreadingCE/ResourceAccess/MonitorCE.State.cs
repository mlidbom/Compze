using System;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   public static MonitorCE WithDefaultTimeout() => new(DefaultTimeout);
   internal static MonitorCE WithInfiniteTimeout() => new(InfiniteTimeout);
   internal static MonitorCE WithTimeout(TimeSpan timeout) => new(timeout);

   readonly object _lockObject = new();

   //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.
   readonly ReadLock _readLock;
   readonly UpdateLock _updateLock;

   static readonly TimeSpan InfiniteTimeout = -1.Milliseconds();
#if NCRUNCH
        static readonly TimeSpan DefaultTimeout = 45.Seconds(); //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
#else
   static readonly TimeSpan DefaultTimeout = 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is all but guaranteed that we have an in-memory deadlock.
#endif

   readonly TimeSpan _timeout;
   TimeSpan? _stackTraceFetchTimeout;

   MonitorCE(TimeSpan timeout)
   {
      _readLock = new ReadLock(this);
      _stackTraceFetchTimeout = null;
      _updateLock = new UpdateLock(this);
      _timeout = timeout;
   }
}