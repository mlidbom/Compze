namespace Compze.Internals.Logging;

public static class ExceptionLogger
{
   public static TResult ExceptionsAndRethrow<TResult>(this ILogger log, Func<TResult> func)
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