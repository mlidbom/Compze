using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;

namespace Compze.Threading;

///<summary>Async version of <see cref="RunOnce"/>. Ensures an async action runs exactly once, even when called concurrently. Subsequent callers await the first call's completion.</summary>
public class RunOnceAsync
{
   int _ran;
   readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

   ///<summary>Returns true if this is the first call, false for all subsequent calls. Thread-safe.</summary>
   public bool IsFirstCall() => _ran == 0 && Interlocked.CompareExchange(ref _ran, 1, 0) == 0;

   ///<summary>Executes <paramref name="action"/> if this is the first call. Subsequent callers await the original call's completion. If the first call threw, subsequent callers receive the exception.</summary>
   public async Task RunIfFirstCallAsync(Func<Task> action)
   {
      if(IsFirstCall())
      {
         try
         {
            await action().ConfigureAwait(false);
            _completed.TrySetResult();
         }
         catch(Exception ex)
         {
            _completed.TrySetException(ex);
            throw;
         }
      } else
      {
         await _completed.Task.ConfigureAwait(false);
      }
   }
}
