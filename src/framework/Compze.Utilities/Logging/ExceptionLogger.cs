using System;

namespace Compze.Logging;

static class ExceptionLogger
{
   internal static void LogAndSuppressExceptions(this ILogger log, Action action)
   {
      try
      {
         action();
      }
      catch(Exception e)
      {
         log.Error(e);
      }
   }

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