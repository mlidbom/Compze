using System;
using Compze.Contracts;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

static class MonitorCEExtensions
{
   ///<summary>Implements the double-checked locking pattern which:
   /// 1. Returns the  value from <paramref name="unlockedTryGetValue"/> if it is not null.
   /// 2. Calls <paramref name="lockedSetValue"/> within an update lock if <paramref name="unlockedTryGetValue"/> returns null.
   /// 2.1 Checks once again for the presence of the value after taking the update lock before adding it.
   /// 2.2 Asserts that <paramref name="unlockedTryGetValue"/> returns a value after <paramref name="lockedSetValue"/> has been called.
   /// </summary>
   public static TResult DoubleCheckedLocking<TResult>(this MonitorCE @this, Func<TResult?> unlockedTryGetValue, Action lockedSetValue) where TResult : class
   {
      var result = unlockedTryGetValue();
      if(result != null) return result;
      return @this.Update(() =>
      {
         result = unlockedTryGetValue();
         if(result != null) return result;
         lockedSetValue();
         return Assert.Result.ReturnNotNull(unlockedTryGetValue());
      });
   }
}
