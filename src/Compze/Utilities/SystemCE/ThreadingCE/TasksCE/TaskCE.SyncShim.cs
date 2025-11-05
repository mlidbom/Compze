using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable MethodSupportsCancellation

namespace Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

static partial class TaskCE
{
   ///<summary>
   /// Return Task.Result
   /// If Result throws, and the <see cref="AggregateException"/>.InnerExceptions contains a single Exception, rethrows that single exception while maintaining a proper stack trace.
   /// </summary>
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

   ///<summary>
   /// Calls Task.Wait()
   /// If Wait() throws, and the <see cref="AggregateException"/>.InnerExceptions contains a single Exception, rethrows that single exception while maintaining a proper stack trace.
   /// </summary>
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
}
