using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE;

public class RunOnce
{
   int _ran = 0;
   readonly TaskCompletionSource _completed = new();

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

   public void RunIfFirstCall(Action action)
   {
      if(IsFirstCall())
      {
         try
         {
            action();
            _completed.TrySetResult();
         }
         catch(Exception ex)
         {
            _completed.TrySetException(ex);
            throw;
         }
      } else
      {
         _completed.Task.GetAwaiter().GetResult();
      }
   }
}
