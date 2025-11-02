using System;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class TypeAssertions
{
   public static Must<object> BeOfType<TExpected>(this Must<object> must) =>
      must.Satisfy(it => it.GetType() == typeof(TExpected));

   public static Must<object> BeAssignableTo<TExpected>(this Must<object> must) =>
      must.Satisfy(it => it.GetType().IsAssignableTo(typeof(TExpected)));
}
