namespace Compze.Threading;

///<summary>Extension methods for <see cref="IAwaitableMonitor"/>.</summary>
public static class IAwaitableMonitorDoubleCheckedLocking
{
   ///<summary>First attempts <paramref name="tryRead"/> without a lock. If it returns null, acquires a read lock, retries <paramref name="tryRead"/>, and if still null acquires an update lock to call <paramref name="updateOnFailedRead"/>, then retries once more. Throws if the final read returns null.</summary>
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
