using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Assertions that a collection contains an item.</summary>
public static class ContainmentAssertions
{
   /// <summary>Asserts that the set contains <paramref name="item"/>.</summary>
   public static IAssertionContext<IReadOnlySet<T>> Contain<T>(this IAssertionContext<IReadOnlySet<T>> context, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.RunAssertion(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

   /// <summary>Asserts that the set contains <paramref name="item"/>.</summary>
   public static IAssertionContext<ISet<T>> Contain<T>(this IAssertionContext<ISet<T>> context, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.RunAssertion(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

   /// <summary>Asserts that the set contains <paramref name="item"/>.</summary>
   public static IAssertionContext<HashSet<T>> Contain<T>(this IAssertionContext<HashSet<T>> context, T item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.RunAssertion(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

   /// <summary>Asserts that the sequence contains <paramref name="item"/>.</summary>
   public static IAssertionContext<IEnumerable<TItem>> Contain<TItem>(this IAssertionContext<IEnumerable<TItem>> context, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.RunAssertion(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);

   /// <summary>Asserts that the collection contains <paramref name="item"/>.</summary>
   public static IAssertionContext<ReadOnlyCollection<TItem>> Contain<TItem>(this IAssertionContext<ReadOnlyCollection<TItem>> context, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.RunAssertion(it => it.Contains(item), expressionValues: [new(itemExpression, item)]);
}
