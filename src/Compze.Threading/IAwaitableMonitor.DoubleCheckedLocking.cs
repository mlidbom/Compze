namespace Compze.Threading;

public static class IAwaitableMonitorDoubleCheckedLocking
{
   public static TResult DoubleCheckedLocking<TResult>(this IAwaitableMonitor @this, Func<TResult?> tryRead, Action updateOnFailedRead)
      where TResult : class =>
      tryRead() ?? @this.Read(() =>
      {
         var result = tryRead();
         if(result != null) return result;
         @this.Update(updateOnFailedRead);
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(updateOnFailedRead)} had been called.");
      });
}
