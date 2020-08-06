﻿using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    partial class MonitorCE
    {
        internal static MonitorCE WithDefaultTimeout() => new MonitorCE(DefaultTimeout);
        internal static MonitorCE WithInfiniteTimeout() => new MonitorCE(InfiniteTimeout);
        internal static MonitorCE WithTimeout(TimeSpan defaultTimeout) => new MonitorCE(defaultTimeout);

        int _waitingThreadCount;
        readonly object _lockObject = new object();

        //By creating the locks only once in the constructor usages become zero-allocation operations. By always referencing them by the concrete type inlining remains possible.
        readonly Lock _lock;
        readonly NotifyOneLock _notifyOneLock;
        readonly NotifyAllLock _notifyAllLock;

        internal static readonly TimeSpan InfiniteTimeout = -1.Milliseconds();
#if NCRUNCH
        internal static readonly TimeSpan DefaultTimeout = 45.Seconds(); //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
#else
        internal static readonly TimeSpan DefaultTimeout = 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is all but guaranteed that we have an in-memory deadlock.
#endif

        internal readonly TimeSpan Timeout;

        MonitorCE(TimeSpan timeout)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _lock = new Lock(this);
            _notifyOneLock = new NotifyOneLock(this);
            _notifyAllLock = new NotifyAllLock(this);
#pragma warning restore CS0618 // Type or member is obsolete
            Timeout = timeout;
        }
    }
}
