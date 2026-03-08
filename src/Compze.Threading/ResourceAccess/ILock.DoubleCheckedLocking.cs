namespace Compze.Threading.ResourceAccess;

public static class MonitorCEExtensions
{
   public static TResult DoubleCheckedLocking<TResult>(this ILock @this, Func<TResult?> tryRead, Action updateOnFailedRead)
      where TResult : class =>
      tryRead() ?? @this.Locked(() =>
      {
         var result = tryRead();
         if(result != null) return result;
         updateOnFailedRead();
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(updateOnFailedRead)} had been called.");
      });
}
