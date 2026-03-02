using System;

namespace Compze.Threading.ResourceAccess;

static class AwaitableMonitorCEExtensions
{
   extension(IAwaitableMonitor @this)
   {
      public TResult ReadOrUpdate<TResult>(Func<TResult?> tryRead, Action updateOnFailedRead, LockTimeout? timeout = null)
         where TResult : class =>
         @this.Read(() => tryRead() ?? @this.Update(() =>
         {
            updateOnFailedRead();
            return tryRead() ?? throw new Exception($"{nameof(tryRead)} returned null even after {nameof(updateOnFailedRead)} had been called.");
         }, timeout), timeout);
   }
}
