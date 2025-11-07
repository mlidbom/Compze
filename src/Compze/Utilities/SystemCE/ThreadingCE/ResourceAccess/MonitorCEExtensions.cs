using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

static class MonitorCEExtensions
{
   ///<summary>Implements the double-checked locking pattern which:
   /// 1. Returns the  value from <paramref name="tryRead"/> if it is not null.
   /// 2. Calls <paramref name="update"/> within an update lock if <paramref name="tryRead"/> returns null.
   /// 2.1 Checks once again for the presence of the value after taking the update lock before adding it.
   /// 2.2 Asserts that <paramref name="tryRead"/> returns a value after <paramref name="update"/> has been called.
   /// </summary>
   public static TResult DoubleCheckedLocking<TResult>(this IMonitorCE @this, Func<TResult?> tryRead, Action update)
      where TResult : class =>
      tryRead() ?? @this.Update(() =>
      {
         var result = tryRead();
         if(result != null) return result;
         update();
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(update)} had been called.");
      });

   public static TResult ReadOrUpdate<TResult>(this IMonitorCE @this, Func<TResult?> tryRead, Action update, TimeSpan? timeout = null)
      where TResult : class =>
      @this.Read(() => tryRead() ?? @this.Update(() =>
      {
         update();
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(update)} had been called.");
      }));
}
