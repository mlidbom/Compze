using System.Runtime.CompilerServices;
using Compze.Must._private;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Assertions over enumerable sequences.</summary>
/// <remarks>
/// Each takes the subject as an <see cref="IEnumerable{T}"/> of its element type and returns the context as one, as<br/>
/// <see cref="ContainmentAssertions.Contain{TItem}"/> does. The element type comes from the subject, so the expected values<br/>
/// need no type of their own: <c>[]</c>, values of a narrower type than the subject's elements, and values of several types<br/>
/// all compile. The subject's own collection type, such as <see cref="List{T}"/>, is not kept for the assertions after it.
/// </remarks>
public static class EnumerableAssertions
{
   /// <summary>Asserts that the sequence contains exactly <paramref name="count"/> elements. A <see langword="null"/> sequence fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<IEnumerable<TElement>> HaveCount<TElement>(this IAssertionContext<IEnumerable<TElement>?> context, int count, [CallerArgumentExpression(nameof(count))] string countExpression = null!)
      => context.RunAssertionThatANullValueFails(it => it.Count() == count,
                                                 failureMessage: it => $"Expected count to be {count} but it was {it.Count()}",
                                                 expressionValues: [new(countExpression, count)]);

   /// <summary>Asserts that the sequence is empty. A <see langword="null"/> sequence is not empty: it fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<IEnumerable<TElement>> BeEmpty<TElement>(this IAssertionContext<IEnumerable<TElement>?> context)
      => context.RunAssertionThatANullValueFails(it => !it.Any());

   /// <summary>Asserts that the sequence contains at least one element. A <see langword="null"/> sequence fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<IEnumerable<TElement>> NotBeEmpty<TElement>(this IAssertionContext<IEnumerable<TElement>?> context)
      => context.RunAssertionThatANullValueFails(it => it.Any());

   //Todo: rename
   /// <summary>
   /// Asserts that the sequence equals <paramref name="expected"/> element-by-element, rendering a diff on failure. A <see langword="null"/> sequence<br/>
   /// fails, so the returned context is non-nullable. A <see langword="null"/> <paramref name="expected"/> is a bug in the test: it throws an <see cref="ArgumentNullException"/>.
   /// </summary>
   public static IAssertionContext<IEnumerable<TElement>> SequenceEqual<TElement>(this IAssertionContext<IEnumerable<TElement>?> context, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      Argument.NotNull(expected, expectedExpression);
      return context.RunAssertionThatANullValueFails(
         it => it.SequenceEqual(expected),
         messageOverride: _ =>
            $"""
             {context.FailingAssertionHeading(nameof(SequenceEqual), [new(expectedExpression, expected)])}
             {context.Diff(expected, context.Actual)}
             {context.ExpressionValue()}
             {context.ExpressionValue(expectedExpression, expected)}
             """);
   }
}
