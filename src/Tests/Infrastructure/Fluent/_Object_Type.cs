using System;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class TypeAssertions
{
   public static Must<TExpected> BeOfType<TExpected>(this Must must) =>
      must.Satisfy(it => it.GetType() == typeof(TExpected))
          .Cast<TExpected>();

   public static Must<TExpected> BeAssignableTo<TExpected>(this Must must) =>
      must.Satisfy(it => it.GetType().IsAssignableTo(typeof(TExpected)))
          .Cast<TExpected>();
}
