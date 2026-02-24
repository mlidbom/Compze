using System;
using System.Threading.Tasks;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE;

public static class SyncOrAsyncCE
{
   public static Func<Task<TResult>> AsAsync<TResult>(this Func<TResult> func) =>
      () => Task.FromResult(func());

   public static Func<Task<unit>> AsAsync(this Action action) =>
      () =>
      {
         action();
         return Task.FromResult(unit.Value);
      };
}
