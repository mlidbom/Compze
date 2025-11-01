using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Enumerables_lists
{
   public static Must<TCollection> HaveCount<TCollection>(this Must<TCollection> must, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : IReadOnlyCollection<object>
      => must.Satisfy(it => it.Count == count, predicateExpression:$"Count == {predicateExpression}", failureMessage: it => $"but Count was: {it.Count}, not {count}");

   public static Must<TCollection> BeEmpty<TCollection>(this Must<TCollection> must, string? message = null!)
      where TCollection : IEnumerable<object>
      => must.Satisfy(it => !it.Any(), failureMessage: it => $"but it contained {it.Count()} items");

   public static Must<TCollection> NotBeEmpty<TCollection>(this Must<TCollection> must, string? message = null!)
      where TCollection : IEnumerable<object>
      => must.Satisfy(it => it.Any());
}
