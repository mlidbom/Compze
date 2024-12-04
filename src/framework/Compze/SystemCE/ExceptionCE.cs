using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Contracts;

namespace Compze.SystemCE;

///<summary>Extensions for working with extensions</summary>
static class ExceptionCE
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
}
