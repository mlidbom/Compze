using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public static class MonitorCEExtensions
{
   public static TResult DoubleCheckedLocking<TResult>(this IMonitorCE @this, Func<TResult?> tryRead, Action updateOnFailedRead)
      where TResult : class =>
      tryRead() ?? @this.Locked(() =>
      {
         var result = tryRead();
         if(result != null) return result;
         updateOnFailedRead();
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(updateOnFailedRead)} had been called.");
      });

   public static TResult DoubleCheckedLocking<TResult>(this IMonitorCE @this, Func<bool> canRead, Func<TResult> read, Action updateOnFailedRead) =>
      canRead()
         ? read()
         : @this.Locked(() =>
         {
            if(canRead()) return read();
            updateOnFailedRead();
            return read();
         });
}
