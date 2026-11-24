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
#pragma warning disable VSTHRD002 // The bridge itself: each caller waits synchronously, and has its todo:urgent where it calls.
         return task.Result;
#pragma warning restore VSTHRD002
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
#pragma warning disable VSTHRD002 // The bridge itself: each caller waits synchronously, and has its todo:urgent where it calls.
         task.Wait();
#pragma warning restore VSTHRD002
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
