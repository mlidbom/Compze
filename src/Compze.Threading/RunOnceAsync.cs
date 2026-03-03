using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Threading;

public class RunOnceAsync
{
   int _ran;
   readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

   public bool IsFirstCall() => _ran == 0 && Interlocked.CompareExchange(ref _ran, 1, 0) == 0;

   public async Task RunIfFirstCallAsync(Func<Task> action)
   {
      if(IsFirstCall())
      {
         try
         {
            await action().caf();
            _completed.TrySetResult();
         }
         catch(Exception ex)
         {
            _completed.TrySetException(ex);
            throw;
         }
      } else
      {
         await _completed.Task.caf();
      }
   }
}
