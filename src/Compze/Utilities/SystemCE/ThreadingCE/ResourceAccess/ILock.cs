using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

//THis file contains the core, the functionality implemented by the actual ILock implementation, upon which everything in the other files is convenience extensions
public partial interface ILock
{
   public static ILock WithDefaultTimeout() => LockCE.WithDefaultTimeout();
   public static ILock WithTimeout(TimeSpan timeout) => LockCE.WithTimeout(timeout);

   TimeSpan Timeout { get; }

   IDisposable TakeReadLock(TimeSpan timeout);
   IDisposable TakeUpdateLock(TimeSpan timeout);

   IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition);
   IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition);

   bool TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)]out IDisposable? updateLock)
   {
      updateLock = TryTakeUpdateLockWhen(timeout, condition);
      return updateLock != null;
   }

   bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? readLock)
   {
      readLock = TryTakeReadLockWhen(timeout, condition);
      return readLock != null;
   }
}
