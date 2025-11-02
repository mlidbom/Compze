using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Fluent;

public static class Must_Contain
{
   public static IMust<IReadOnlySet<T>> Contain<T>(this IMust<IReadOnlySet<T>> must, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => must.Satisfy(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

   public static IMust<ISet<T>> Contain<T>(this IMust<ISet<T>> must, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => must.Satisfy(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

    public static IMust<HashSet<T>> Contain<T>(this IMust<HashSet<T>> must, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
       => must.Satisfy(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

    public static IMust<IEnumerable<TItem>> Contain<TItem>(this IMust<IEnumerable<TItem>> must, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => must.Satisfy(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

   public static IMust<ReadOnlyCollection<TItem>> Contain<TItem>(this IMust<ReadOnlyCollection<TItem>> must, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => must.Satisfy(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

}
