using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must_Contain
{
   public static IAssertionContext<IReadOnlySet<T>> Contain<T>(this IAssertionContext<IReadOnlySet<T>> context, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.SatisfyInternal(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

   public static IAssertionContext<ISet<T>> Contain<T>(this IAssertionContext<ISet<T>> context, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.SatisfyInternal(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

    public static IAssertionContext<HashSet<T>> Contain<T>(this IAssertionContext<HashSet<T>> context, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
       => context.SatisfyInternal(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

    public static IAssertionContext<IEnumerable<TItem>> Contain<TItem>(this IAssertionContext<IEnumerable<TItem>> context, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.SatisfyInternal(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

   public static IAssertionContext<ReadOnlyCollection<TItem>> Contain<TItem>(this IAssertionContext<ReadOnlyCollection<TItem>> context, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.SatisfyInternal(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

}
