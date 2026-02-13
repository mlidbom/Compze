using System;
using System.Threading.Tasks;

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

public static class TaskCEExceptionHandling
{
   /// <summary>
   /// Ensures that if this task fails, the thrown exception is an taggregate exception
   /// That way catching code does not need to first figure out whether the exception is an taggregate exception or not
   /// </summary>
   public static async Task WithAggregateExceptions(this Task task)
   {
      try
      {
         await task.AsUnit().caf();
      }
      catch(Exception exception)
      {
         throw task.Exception ?? new AggregateException(exception);
      }
   }

   /// <summary>
   /// Ensures that if this task fails, the thrown exception is an taggregate exception
   /// That way catching code does not need to first figure out whether the exception is an taggregate exception or not
   /// </summary>
   public static async Task WithAggregateExceptions(this ValueTask valueTask) => await valueTask.AsTask().WithAggregateExceptions().caf();
}
