using System;

namespace Compze.Threading.ResourceAccess;

static class AwaitableMonitorCEExtensions
{
   extension(IAwaitableMonitor @this)
   {
      public TResult ReadOrUpdate<TResult>(Func<TResult?> tryRead, Action updateOnFailedRead)
         where TResult : class =>
         @this.Read(() => tryRead() ?? @this.Update(() =>
         {
            updateOnFailedRead();
            return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(updateOnFailedRead)} had been called.");
         }));

      public TResult ReadOrUpdate<TResult>(Func<bool> canRead, Func<TResult> read, Action update) =>
         @this.Read(() => canRead()
                             ? read()
                             : @this.Update(() =>
                             {
                                update();
                                return read();
                             }));
   }
}
