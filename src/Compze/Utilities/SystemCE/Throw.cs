using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE;

static class Throw<TException> where TException : Exception, new()
{
   public static unit If(bool condition) => condition ? throw new TException() : unit.Value;
}
