namespace Compze.Utilities.SystemCE.ThreadingCE;

public static class SyncOrAsyncCE
{
   public static Func<Task<TResult>> AsAsync<TResult>(this Func<TResult> func) =>
      () => Task.FromResult(func());

   internal static Func<Task<unit>> AsAsync(this Action action) =>
      () =>
      {
         action();
         return Task.FromResult(unit.Value);
      };
}
