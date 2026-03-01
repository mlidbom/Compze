using System;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE;

public static class Throw<TException> where TException : Exception, new()
{
   public static unit If(bool condition) => unit.From(() =>
   {
      if(condition) throw new TException();
   });
}
