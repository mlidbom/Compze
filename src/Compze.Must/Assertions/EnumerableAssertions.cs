using System.Runtime.CompilerServices;
using Compze.Must._private;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Assertions over enumerable sequences.</summary>
public static class EnumerableAssertions
{
   /// <summary>Asserts that the sequence contains exactly <paramref name="count"/> elements. A <see langword="null"/> sequence fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<TCollection> HaveCount<TCollection>(this IAssertionContext<TCollection?> context, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : System.Collections.IEnumerable
      => context.RunAssertion(it => it is not null && it.Cast<object>().Count() == count,
                              failureMessage: it => $"Expected count to be {count} but it was {it?.Cast<object>().Count().ToStringInvariant() ?? "null"}",
                              expressionValues: [new(predicateExpression, count)])!;

   /// <summary>Asserts that the sequence is empty. A <see langword="null"/> sequence is not empty: it fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<TCollection> BeEmpty<TCollection>(this IAssertionContext<TCollection?> context)
      where TCollection : System.Collections.IEnumerable
      => context.RunAssertion(it => it is not null && !it.Cast<object>().Any())!;

   /// <summary>Asserts that the sequence contains at least one element. A <see langword="null"/> sequence fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<TCollection> NotBeEmpty<TCollection>(this IAssertionContext<TCollection?> context)
      where TCollection : System.Collections.IEnumerable
      => context.RunAssertion(it => it is not null && it.Cast<object>().Any())!;

   //Todo: rename
   /// <summary>Asserts that the sequence equals <paramref name="expected"/> element-by-element, rendering a diff on failure. A <see langword="null"/> sequence fails, so the returned context is non-nullable.</summary>
   public static IAssertionContext<TCollection> SequenceEqual<TCollection, TElement>(this IAssertionContext<TCollection?> context, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TCollection : IEnumerable<TElement>
   {
      return context.RunAssertion(
         it => it is not null && it.SequenceEqual(expected),
         messageOverride: _ =>
            $"""
             {context.FailingAssertionHeading(nameof(SequenceEqual), [new(expectedExpression, expected)])}
             {context.Diff(expected, context.Actual)}
             {context.ExpressionValue()}
             {context.ExpressionValue(expectedExpression, expected)}
             """)!;
   }
}
