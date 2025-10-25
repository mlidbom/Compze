using System;

namespace Compze.Utilities.Logging;

static class ExceptionLogger
{
   internal static TResult ExceptionsAndRethrow<TResult>(this ILogger log, Func<TResult> func)
   {
      try
      {
         return func();
      }
      catch(Exception e)
      {
         log.Error(e);
         throw;
      }
   }
}