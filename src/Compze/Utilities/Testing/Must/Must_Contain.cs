using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must_Contain
{
   public static IAssertionContext<IReadOnlySet<T>> Contain<T>(this IAssertionContext<IReadOnlySet<T>> assertionContext, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => assertionContext.SatisfyInternal(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

   public static IAssertionContext<ISet<T>> Contain<T>(this IAssertionContext<ISet<T>> assertionContext, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => assertionContext.SatisfyInternal(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

    public static IAssertionContext<HashSet<T>> Contain<T>(this IAssertionContext<HashSet<T>> assertionContext, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
       => assertionContext.SatisfyInternal(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

    public static IAssertionContext<IEnumerable<TItem>> Contain<TItem>(this IAssertionContext<IEnumerable<TItem>> assertionContext, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => assertionContext.SatisfyInternal(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

   public static IAssertionContext<ReadOnlyCollection<TItem>> Contain<TItem>(this IAssertionContext<ReadOnlyCollection<TItem>> assertionContext, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => assertionContext.SatisfyInternal(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

}
