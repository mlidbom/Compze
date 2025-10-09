using System;

namespace Compze.Tests.Infrastructure.SystemCE;

static class ExceptionCE
{
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
}
