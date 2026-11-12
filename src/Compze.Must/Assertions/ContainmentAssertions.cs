using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>The assertion that a collection contains an item. A <see langword="null"/> collection fails, so it returns a non-nullable context.</summary>
/// <remarks>
/// It takes the subject as an <see cref="IEnumerable{T}"/> of its element type and returns the context as one, as<br/>
/// <see cref="EnumerableAssertions"/> do: the element type comes from the subject, so an item of a narrower type compiles.
/// </remarks>
public static class ContainmentAssertions
{
   /// <summary>Asserts that the collection contains <paramref name="item"/>, as the collection itself decides: a set by its own comparer.</summary>
   public static IAssertionContext<IEnumerable<TItem>> Contain<TItem>(this IAssertionContext<IEnumerable<TItem>?> context, TItem item, [CallerArgumentExpression(nameof(item))] string itemExpression = null!)
      => context.RunAssertionThatANullValueFails(it => ContainsAsTheCollectionItselfDecides(it, item), expressionValues: [new(itemExpression, item)]);

   /// <summary>
   /// Whether <paramref name="collection"/> contains <paramref name="item"/>, asking the collection itself where it can answer.<br/>
   /// <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource}, TSource)"/> asks an <see cref="ICollection{T}"/>, which every .NET set is;<br/>
   /// a read-only set that isn't one would otherwise be searched by the default equality, not by its own comparer.
   /// </summary>
   static bool ContainsAsTheCollectionItselfDecides<TItem>(IEnumerable<TItem> collection, TItem item) =>
      collection is IReadOnlySet<TItem> readOnlySet ? readOnlySet.Contains(item) : collection.Contains(item);
}
