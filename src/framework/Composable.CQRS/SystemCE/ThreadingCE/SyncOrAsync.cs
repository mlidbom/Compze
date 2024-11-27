using System;
using System.Threading.Tasks;
using Composable.Contracts;

namespace Composable.SystemCE.ThreadingCE;

static class SyncOrAsyncCE
{
   internal static Func<Task<TResult>> AsAsync<TResult>(this Func<TResult> func) =>
      () => Task.FromResult(func());

   internal static TResult SyncResult<TResult>(this Task<TResult> @this)
   {
      //Should only ever be called when in the sync mode, so assert that the task is done.
      Assert.Argument.Assert(@this.IsCompleted);
      return @this.GetAwaiter().GetResult();
   }
}
