using System;

namespace Compze.Threading.ResourceAccess;

public static class AwaitableMonitorCEExtensions
{
   public static TResult ReadOrUpdate<TResult>(this IAwaitableMonitor @this, Func<TResult?> tryRead, Action updateOnFailedRead)
      where TResult : class =>
      @this.Read(() => tryRead() ?? @this.Update(() =>
      {
         updateOnFailedRead();
         return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(updateOnFailedRead)} had been called.");
      }));

   public static TResult ReadOrUpdate<TResult>(this IAwaitableMonitor @this, Func<bool> canRead, Func<TResult> read, Action update) =>
      @this.Read(() => canRead()
                          ? read()
                          : @this.Update(() =>
                          {
                             update();
                             return read();
                          }));
}
