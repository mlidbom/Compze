using System;
using System.Threading.Tasks;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE;

static class SyncOrAsyncCE
{
   internal static Func<Task<TResult>> AsAsync<TResult>(this Func<TResult> func) =>
      () => Task.FromResult(func());

   internal static Func<Task<unit>> AsAsync(this Action action) =>
      () =>
      {
         action();
         return Task.FromResult(unit.Value);
      };
}
