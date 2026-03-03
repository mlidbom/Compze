namespace Compze.Tests.Infrastructure.SystemCE;

public static class ExceptionCE
{
   public static Exception? TryCatch(Action action)
   {
      try
      {
         action();
      }
#pragma warning disable CA1031 //Here we catch all exceptions so we can give them back to the client
      catch(Exception e)
#pragma warning restore CA1031 //Here we catch all exceptions so we can give them back to the client
      {
         return e;
      }

      return null;
   }
}
