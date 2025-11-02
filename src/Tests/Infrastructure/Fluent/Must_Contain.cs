using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must_Contain
{
   //Todo: write tests
   public static IMust<ISet<T>> Contain<T>(this IMust<ISet<T>> must, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => must.Satisfy(it => it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

   //Todo: write tests
   public static IMust<TCollection> Contain<TCollection, TItem>(this IMust<TCollection> must, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      where TCollection : IEnumerable<TItem>
      => must.Satisfy(it => !it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

   public static IMust<ReadOnlyCollection<TItem>> Contain<TItem>(this IMust<ReadOnlyCollection<TItem>> must, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => must.Satisfy(it => !it.Contains(item), usedArguments: [new(nameof(item), itemExpression, item)]);

}
