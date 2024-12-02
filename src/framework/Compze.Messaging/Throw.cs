using System;
using Compze.Functional;

namespace Compze;

static class Throw<TException> where TException : Exception, new()
{
   public static Unit If(bool condition) => condition ? throw new TException() : Unit.Instance;
}
