using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must___Enumerable
{
   public static IAssertionContext<TCollection> HaveCount<TCollection>(this IAssertionContext<TCollection> context, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : System.Collections.IEnumerable
      => context.SatisfyInternal(it => it.Cast<object>().Count() == count, failureMessage: it => $"Expected count to be {count} but it was {it.Cast<IEnumerable<object>>().Count()}", expressionValues: [new (predicateExpression, count)]);

   public static IAssertionContext<TCollection> BeEmpty<TCollection>(this IAssertionContext<TCollection> context)
      where TCollection : System.Collections.IEnumerable
      => context.SatisfyInternal(it => !it.Cast<object>().Any());

   public static IAssertionContext<TCollection> NotBeEmpty<TCollection>(this IAssertionContext<TCollection> context)
      where TCollection : System.Collections.IEnumerable
      => context.SatisfyInternal(it => it.Cast<object>().Any());

   //Todo: rename
   public static IAssertionContext<TCollection> SequenceEqual<TCollection, TElement>(this IAssertionContext<TCollection> context, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TCollection : IEnumerable<TElement>
   {
      return context.SatisfyInternal(
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
