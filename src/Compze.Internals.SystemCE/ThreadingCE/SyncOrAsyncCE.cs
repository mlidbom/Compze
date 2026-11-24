using Compze.SystemCE;

namespace Compze.Internals.SystemCE.ThreadingCE;

public static class SyncOrAsyncCE
{
   ///<summary>The function as an async one, which runs it when called and returns its result as a completed task: for code that<br/>
   /// takes an async function, called with a synchronous one.</summary>
   ///<remarks>review-naming: ToCompletedTaskFunc, was AsAsync: it returns a function, not a task, so VSTHRD200 asks its name not to end in<br/>
   /// Async; not ToAsyncFunc, which Compze.Unit has for another conversion (state-store-in-tables, 2026-11-24).</remarks>
   public static Func<Task<TResult>> ToCompletedTaskFunc<TResult>(this Func<TResult> func) =>
      () => Task.FromResult(func());

   internal static Func<Task<Unit>> ToCompletedTaskFunc(this Action action) =>
      () =>
      {
         action();
         return Task.FromResult(unit);
      };
}
