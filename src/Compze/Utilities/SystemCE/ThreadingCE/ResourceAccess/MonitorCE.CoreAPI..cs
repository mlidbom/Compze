using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE : IMonitorCE
{
   public IDisposable? TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition) => TryEnterWhen(timeout, condition) ? _updateLock : null;

   public IDisposable? TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition)  => TryEnterWhen(timeout, condition) ? _readLock : null;

   public bool TryTakeUpdateLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)]out IDisposable? takenLock)
   {
      if(TryEnterWhen(timeout, condition))
      {
         takenLock = _updateLock;
         return true;
      } else
      {
         takenLock = null;
         return false;
      }
   }

   public bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)]out IDisposable? takenLock)
   {
      if(TryEnterWhen(timeout, condition))
      {
         takenLock = _readLock;
         return true;
      } else
      {
         takenLock = null;
         return false;
      }
   }
}
