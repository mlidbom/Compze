using System;
using Compze.Contracts;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

static class MonitorCEExtensions
{
   ///<summary>Implements the double-checked locking pattern which:
   /// 1. Returns the  value from <paramref name="tryGetValue"/> if it is not null.
   /// 2. Calls <paramref name="setValue"/> within an update lock if <paramref name="tryGetValue"/> returns null.
   /// 2.1 Checks once again for the presence of the value after taking the update lock before adding it.
   /// 2.2 Asserts that <paramref name="tryGetValue"/> returns a value after <paramref name="setValue"/> has been called.
   /// </summary>
   public static TResult DoubleCheckedLocking<TResult>(this MonitorCE @this, Func<TResult?> tryGetValue, Action setValue) where TResult : class
   {
      var result = tryGetValue();
      if(result != null) return result;
      return @this.Update(() =>
      {
         result = tryGetValue();
         if(result != null) return result;
         setValue();
         return Assert.Result.ReturnNotNull(tryGetValue());
      });
   }
}
