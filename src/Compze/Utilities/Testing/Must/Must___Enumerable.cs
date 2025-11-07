using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Must.Serialization;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Compze.Utilities.Testing.Must;

public static class Must___Enumerable
{
   public static IAssertionContext<TCollection> HaveCount<TCollection>(this IAssertionContext<TCollection> context, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : System.Collections.IEnumerable
      => context.SatisfyInternal(it => it.Cast<object>().Count() == count, failureMessage: it => $"Expected count to be {count} but it was {it.Cast<IEnumerable<object>>().Count()}", expressionValues: [new (predicateExpression, count)]);

   public static IAssertionContext<TCollection> BeEmpty<TCollection>(this IAssertionContext<TCollection> context, string? message = null!)
      where TCollection : System.Collections.IEnumerable
      => context.SatisfyInternal(it => !it.Cast<object>().Any(), failureMessage: it => $"but it contained {it.Cast<object>().Count()} items");

   public static IAssertionContext<TCollection> NotBeEmpty<TCollection>(this IAssertionContext<TCollection> context, string? message = null!)
      where TCollection : System.Collections.IEnumerable
      => context.SatisfyInternal(it => it.Cast<object>().Any());

   //Todo: rename
   public static IAssertionContext<TCollection> SequenceEqual<TCollection, TElement>(this IAssertionContext<TCollection> context, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TCollection : IEnumerable<TElement>
   {
      var actualJson = JsonConvert.SerializeObject(context.Actual, TestingJsonSettings.AllMembers);
      var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);

      return context.SatisfyInternal(
         it => it.SequenceEqual(expected),
         messageOverride: _ =>
            $"""
             {AssertionContext.Separator}
             expected the sequence:
             {AssertionContext.Separator}
             {context.Expression.Indent()}
             {AssertionContext.Separator}
             to be sequence equal to:
             {AssertionContext.Separator}
             {context.NormalizeExpressionIndentation(expectedExpression).Indent()}
             {AssertionContext.Separator}
             But it was not.
             {AssertionContext.Separator}
             Diff:
             {AssertionContext.Separator}
             {DiffGenerator.CreateDiff(expected: expectedJson, actual: actualJson)}
             {AssertionContext.Separator}
             Actual was:
             {AssertionContext.Separator}
             {actualJson}
             {AssertionContext.Separator}
             Expected was:
             {AssertionContext.Separator}
             {expectedJson}
             {AssertionContext.Separator}
             """);
   }
}
