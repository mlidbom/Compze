using System;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Testing;

public static class AssertThrows
{
   public static async Task<TException> Async<TException>([JetBrains.Annotations.InstantHandle]Func<Task> action) where TException : Exception
   {
      try
      {
         await action().CaF();
      }
      catch(TException exception)
      {
         return exception;
      }
      catch(Exception anyException)
      {
         throw new Exception($"Expected exception of type: {typeof(TException)}, but thrown exception is: {anyException.GetType()}", anyException);
      }

      throw new Exception($"Expected exception of type: {typeof(TException)}, but no exception was thrown");
   }
}