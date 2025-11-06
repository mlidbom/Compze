using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

//THis file contains the methods that implementations actually have to implement
public partial interface ILock
{
   public static ILock WithDefaultTimeout() => LockCE.WithDefaultTimeout();
   public static ILock WithTimeout(TimeSpan timeout) => LockCE.WithTimeout(timeout);

   TimeSpan Timeout { get; }

   IDisposable TakeReadLock(TimeSpan timeout);
   IDisposable TakeUpdateLock(TimeSpan timeout);

   IDisposable TakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable TakeReadLockWhen(TimeSpan timeout, Func<bool> condition);

   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);


   //review: do wo want this exposed? 
   void SetTimeToWaitForStackTrace(TimeSpan timeToWaitForStackTrace);
}
