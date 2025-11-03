using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable MethodSupportsCancellation

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

static partial class TaskCE
{
   internal static TResult ResultUnwrappingException<TResult>(this Task<TResult> task)
   {
      try
      {
         return task.Result;
      }
      catch(AggregateException exception)
      {
         if(exception.InnerExceptions.Count == 1 && exception.InnerException != null)
         {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
         } else
         {
            throw;
         }
      }

      throw new Exception("Impossible!");
   }

   internal static void WaitUnwrappingException(this ValueTask task) => task.AsTask().WaitUnwrappingException();

   internal static void WaitUnwrappingException(this Task task)
   {
      try
      {
         task.Wait();
         return;
      }
      catch(AggregateException exception)
      {
         if(exception.InnerExceptions.Count == 1 && exception.InnerException != null)
         {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
         } else
         {
            throw;
         }
      }

      throw new Exception("Impossible!");
   }

   ///<summary>Task.ContinueWith may run the continuation synchronously on the calling thread, causing all kinds of havoc. This simply guarantees that this does not happen.</summary>
   internal static Task ContinueWithAsynchronously(this Task @this, Action<Task> continuation) =>
      @this.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
}
