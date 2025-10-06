using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities;

static class Throw<TException> where TException : Exception, new()
{
   public static Unit If(bool condition) => condition ? throw new TException() : Unit.Instance;
}
