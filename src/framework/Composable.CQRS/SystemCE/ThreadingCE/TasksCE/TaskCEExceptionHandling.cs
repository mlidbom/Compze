using System;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE;

static class TaskCEExceptionHandling
{
   public static async Task WithAggregateExceptions(this Task task)
   {
      try
      {
         await task.CaF();
      }
      catch(Exception exception)
      {
         throw task.Exception ?? new AggregateException(exception);
      }
   }
}
