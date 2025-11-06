using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

//THis file contains the methods that implementations actually have to implement
public partial interface ILock
{
   public static ILock WithDefaultTimeout() => LockCE.WithDefaultTimeout();
   public static ILock WithTimeout(TimeSpan timeout) => LockCE.WithTimeout(timeout);

   TimeSpan Timeout { get; }

   IDisposable TakeReadLock(TimeSpan timeout);
   IDisposable TakeUpdateLock(TimeSpan timeout);

   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);
}
