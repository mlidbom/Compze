using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.SystemCE;

public class RunOnce
{
   int _ran = 0;
   public bool IsFirstCall() => Volatile.Read(ref _ran) == 0 && Interlocked.CompareExchange(ref _ran, 1, 0) == 0;

   public async Task RunIfFirstCallAsync(Func<Task> action)
   {
      if(IsFirstCall())
      {
         await action().caf();
      }
   }

   public void RunIfFirstCall(Action action)
   {
      if(IsFirstCall())
      {
         action();
      }
   }
}
