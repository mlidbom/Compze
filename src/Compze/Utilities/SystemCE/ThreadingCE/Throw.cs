using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE;

static class Throw<TException> where TException : Exception, new()
{
   internal static unit If(bool condition) => unit.From(() =>
   {
      if(condition) throw new TException();
   });
}
