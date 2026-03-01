using System;
using Compze.Underscore;

namespace Compze.Utilities.SystemCE;

public static class Throw<TException> where TException : Exception, new()
{
   public static unit If(bool condition) => condition ? throw new TException() : unit.Value;
}
