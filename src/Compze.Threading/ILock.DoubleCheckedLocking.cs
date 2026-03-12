namespace Compze.Threading;

///<summary>Extension methods for <see cref="ILock"/>.</summary>
public static class MonitorCEExtensions
{
   ///<summary>First attempts <paramref name="tryRead"/> without the lock. If it returns null, acquires the lock, retries <paramref name="tryRead"/>, and if still null calls <paramref name="updateOnFailedRead"/> then retries once more. Throws if the final read returns null.</summary>
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
