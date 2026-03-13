using System.Runtime.ExceptionServices;

// ReSharper disable MethodSupportsCancellation

namespace Compze.Internals.SystemCE.ThreadingCE.TasksCE;

public static partial class TaskCE
{
   ///<summary>
   /// Return Task.ReturnValue
   /// If ReturnValue throws, and the <see cref="AggregateException"/>.InnerExceptions contains a single Exception, rethrows that single exception while maintaining a proper stack trace.
   /// </summary>
   public static TResult ResultUnwrappingException<TResult>(this Task<TResult> task)
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
   public static void WaitUnwrappingException(this ValueTask task) => task.AsTask().WaitUnwrappingException();

   public static void WaitUnwrappingException(this Task task)
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
