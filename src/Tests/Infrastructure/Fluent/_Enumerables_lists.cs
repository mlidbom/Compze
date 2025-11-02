using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Enumerables_lists
{
   public static Must<TCollection> HaveCount<TCollection>(this Must<TCollection> must, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : System.Collections.IEnumerable
      => must.Satisfy(it => it.Cast<object>().Count() == count, predicateExpression:$"Count == {predicateExpression}", failureMessage: it => $"but Count was: {it.Cast<object>().Count()}, not {count}");

   public static Must<TCollection> BeEmpty<TCollection>(this Must<TCollection> must, string? message = null!)
      where TCollection : System.Collections.IEnumerable
      => must.Satisfy(it => !it.Cast<object>().Any(), failureMessage: it => $"but it contained {it.Cast<object>().Count()} items");

   public static Must<TCollection> NotBeEmpty<TCollection>(this Must<TCollection> must, string? message = null!)
      where TCollection : System.Collections.IEnumerable
      => must.Satisfy(it => it.Cast<object>().Any());

   public static Must<TCollection> SequenceEqual<TCollection, TElement>(this Must<TCollection> must, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TCollection : IEnumerable<TElement>
      => must.Satisfy(it => it.SequenceEqual(expected),
                      usedArguments: [new(nameof(expected), expectedExpression, expected)]);
}
