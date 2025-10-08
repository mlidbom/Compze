using System;

namespace Compze.Utilities;

static class Throw<TException> where TException : Exception, new()
{
   public static unit If(bool condition) => condition ? throw new TException() : unit.Value;
}
