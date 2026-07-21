using Compze.Internals.SystemCE.ThreadingCE.TasksCE.Private;

namespace Compze.Internals.SystemCE.ThreadingCE.TasksCE;

public static class TaskCEExceptionHandling
{
   /// <summary>
   /// Ensures that if this task fails, the thrown exception is an AggregateException
   /// That way catching code does not need to first figure out whether the exception is an AggregateException or not
   /// </summary>
   public static async Task WithAggregateExceptions(this Task task)
   {
      try
      {
         await task.AsUnit().caf();
      }
      catch(Exception exception)
      {
         var taskException = task.Exception ?? new AggregateException(exception);
         // task.Exception always wraps in AggregateException. If the actual thrown exception is already an AggregateException, unwrap the redundant nesting level.
         if(taskException.InnerExceptions is [AggregateException singleInner])
            throw singleInner;
         throw taskException;
      }
   }

   /// <summary>
   /// Ensures that if this task fails, the thrown exception is an AggregateException
   /// That way catching code does not need to first figure out whether the exception is an AggregateException or not
   /// </summary>
   public static async Task WithAggregateExceptions(this ValueTask valueTask) => await valueTask.AsTask().WithAggregateExceptions().caf();
}
