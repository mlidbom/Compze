using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE : IMonitorCE
{
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

   public bool TryTakeReadLockWhen(TimeSpan timeout, Func<bool> condition, [NotNullWhen(true)] out IDisposable? takenLock)
   {
      if (TryEnterWhen(timeout, condition))
      {
         takenLock = _readLock;
         return true;
      }
      else
      {
         takenLock = null;
         return false;
      }
   }
}
