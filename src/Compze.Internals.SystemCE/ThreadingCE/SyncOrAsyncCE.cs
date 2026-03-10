using Compze.SystemCE;

namespace Compze.Internals.SystemCE.ThreadingCE;

public static class SyncOrAsyncCE
{
   public static Func<Task<TResult>> AsAsync<TResult>(this Func<TResult> func) =>
      () => Task.FromResult(func());

   internal static Func<Task<Unit>> AsAsync(this Action action) =>
      () =>
      {
         action();
         return Task.FromResult(unit);
      };
}
