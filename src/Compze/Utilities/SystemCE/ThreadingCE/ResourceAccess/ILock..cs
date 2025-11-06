using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

//THis file contains the methods that implementations actually have to implement
public partial interface ILock
{
#if NCRUNCH
        static readonly TimeSpan DefaultTimeout = 45.Seconds(); //Tests timeout at 60 seconds. We want locks to timeout faster so that the blocking stack traces turn up in the test output so we can diagnose the deadlocks.
#else
   static readonly TimeSpan DefaultTimeout = 2.Minutes(); //MsSql default query timeout is 30 seconds. Default .Net transaction timeout is 60. If we reach 2 minutes it is all but guaranteed that we have an in-memory deadlock.
#endif

   public static ILock WithDefaultTimeout() => new LockCE(DefaultTimeout);
   public static ILock WithTimeout(TimeSpan timeout) => new LockCE(timeout);

   TimeSpan Timeout { get; }

   IDisposable TakeReadLock(TimeSpan timeout);
   IDisposable TakeUpdateLock(TimeSpan timeout);

   IDisposable TakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable TakeReadLockWhen(TimeSpan timeout, Func<bool> condition);

   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);


   //review: do we want this exposed?
   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);
}
