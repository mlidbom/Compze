using System;

namespace Compze.Testing.SystemCE;

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
