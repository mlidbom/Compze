using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

static class MonitorCEExtensions
{
   ///<summary>Implements the double-checked locking pattern which:
   /// 1. Returns the  value from <paramref name="unlockedTryGetValue"/> if it is not null.
   /// 2. Calls <paramref name="setValue"/> within an update lock if <paramref name="unlockedTryGetValue"/> returns null.
   /// 2.1 Checks once again for the presence of the value after taking the update lock before adding it.
   /// 2.2 Asserts that <paramref name="unlockedTryGetValue"/> returns a value after <paramref name="setValue"/> has been called.
   /// </summary>
   public static TResult DoubleCheckedLocking<TResult>(this IMonitorCE @this, Func<TResult?> unlockedTryGetValue, Action setValue) where TResult : class =>
      unlockedTryGetValue() ?? @this.Update(() =>
      {
         var result = unlockedTryGetValue();
         if(result != null) return result;
         setValue();
         return unlockedTryGetValue() ?? throw new Exception($"{nameof(unlockedTryGetValue)} returned null even after {nameof(setValue)} had been called.");;
      });

   public static TResult ReadOrUpdate<TResult>(this IMonitorCE @this, Func<TResult?> tryGetValue, Action setValue, TimeSpan? timeout = null) where TResult : class =>
      @this.Read(() => tryGetValue() ?? @this.Update(() =>
      {
         setValue();
         return tryGetValue() ?? throw new Exception($"{nameof(tryGetValue)} returned null even after {nameof(setValue)} had been called.");
      }));
}
