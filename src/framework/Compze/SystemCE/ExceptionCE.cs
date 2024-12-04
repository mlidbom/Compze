using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Contracts;

namespace Compze.SystemCE;

///<summary>Extensions for working with extensions</summary>
public static class ExceptionCE
{
   ///<summary>Flattens the exception.InnerException hierarchy into a sequence.</summary>
   public static IEnumerable<Exception> GetAllExceptionsInStack(this Exception exception)
   {
      Contract.ArgumentNotNull(exception, nameof(exception));
      var current = exception;
      while(current != null)
      {
         yield return current;
         current = current.InnerException;
      }
   }

   ///<summary>Returns the deepest nested inner exception that was the root cause of the current exception.</summary>
   public static Exception GetRootCauseException(this Exception e) => e.GetAllExceptionsInStack().Last();

   public static Exception? TryCatch(Action action)
   {
      try
      {
         action();
      }
      catch(Exception e)
      {
         return e;
      }

      return null;
   }

   public static bool TryCatch(Action action, [NotNullWhen(true)] out Exception? exception)
   {
      try
      {
         action();
      }
      catch(Exception caught)
      {
         exception = caught;
         return true;
      }

      exception = null;
      return false;
   }

   public static void TryCatch(Action action, Action<Exception> onException)
   {
      try
      {
         action();
      }
      catch(Exception caught)
      {
         onException(caught);
      }
   }

   public static void ThrowIf<TException>(bool condition) where TException : Exception, new()
   {
      if(condition) throw new TException();
   }
}
