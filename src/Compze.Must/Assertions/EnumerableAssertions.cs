using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Must;

/// <summary>Assertions over enumerable sequences.</summary>
public static class EnumerableAssertions
{
   /// <summary>Asserts that the sequence contains exactly <paramref name="count"/> elements.</summary>
   public static IAssertionContext<TCollection> HaveCount<TCollection>(this IAssertionContext<TCollection> context, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : System.Collections.IEnumerable
      => context.RunAssertion(it => it.Cast<object>().Count() == count, failureMessage: it => $"Expected count to be {count} but it was {it.Cast<object>().Count()}", expressionValues: [new(predicateExpression, count)]);

   /// <summary>Asserts that the sequence is empty.</summary>
   public static IAssertionContext<TCollection> BeEmpty<TCollection>(this IAssertionContext<TCollection> context)
      where TCollection : System.Collections.IEnumerable
      => context.RunAssertion(it => !it.Cast<object>().Any());

   /// <summary>Asserts that the sequence contains at least one element.</summary>
   public static IAssertionContext<TCollection> NotBeEmpty<TCollection>(this IAssertionContext<TCollection> context)
      where TCollection : System.Collections.IEnumerable
      => context.RunAssertion(it => it.Cast<object>().Any());

   //Todo: rename
   /// <summary>Asserts that the sequence equals <paramref name="expected"/> element-by-element, rendering a diff on failure.</summary>
   public static IAssertionContext<TCollection> SequenceEqual<TCollection, TElement>(this IAssertionContext<TCollection> context, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TCollection : IEnumerable<TElement>
   {
      return context.RunAssertion(
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
