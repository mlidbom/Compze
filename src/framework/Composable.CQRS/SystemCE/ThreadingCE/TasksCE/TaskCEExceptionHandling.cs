using System;
using System.Threading.Tasks;

namespace Composable.SystemCE.ThreadingCE.TasksCE;

static class TaskCEExceptionHandling
{
   public static async Task WithAggregateExceptions(this Task task)
   {
      try
      {
         await task.AsUnit().CaF();
      }
      catch(Exception exception)
      {
         throw task.Exception ?? new AggregateException(exception);
      }
   }

   public static async Task<T> WithAggregateExceptions<T>(this Task<T> task)
   {
      try
      {
         return await task.CaF();
      }
      catch(Exception exception)
      {
         throw task.Exception ?? new AggregateException(exception);
      }
   }

   public static async Task WithAggregateExceptions(this ValueTask valueTask) => await valueTask.AsTask().WithAggregateExceptions().CaF();
}
