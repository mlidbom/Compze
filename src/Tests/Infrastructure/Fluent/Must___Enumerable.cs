using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace Compze.Tests.Infrastructure.Fluent;

public static class Must___Enumerable
{
   public static Must<TCollection> HaveCount<TCollection>(this Must<TCollection> must, int count, [CallerArgumentExpression(nameof(count))] string predicateExpression = null!)
      where TCollection : System.Collections.IEnumerable
      => must.Satisfy(it => it.Cast<object>().Count() == count, predicateExpression:$"Count == {predicateExpression}", failureMessage: it => $"but Count was: {it.Cast<object>().Count()}, not {count}");

   public static Must<TCollection> BeEmpty<TCollection>(this Must<TCollection> must, string? message = null!)
      where TCollection : System.Collections.IEnumerable
      => must.Satisfy(it => !it.Cast<object>().Any(), failureMessage: it => $"but it contained {it.Cast<object>().Count()} items");

   public static Must<TCollection> NotBeEmpty<TCollection>(this Must<TCollection> must, string? message = null!)
      where TCollection : System.Collections.IEnumerable
      => must.Satisfy(it => it.Cast<object>().Any());

   public static Must<TCollection> SequenceEqual<TCollection, TElement>(this Must<TCollection> must, IEnumerable<TElement> expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TCollection : IEnumerable<TElement>
   {
      var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
      var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);

      return must.Satisfy(
         it => it.SequenceEqual(expected),
         messageOverride: _ =>
            $"""
             {must.Separator}
             expected the sequence:
             {must.Separator}
             {must.Expression.Indent()}
             {must.Separator}
             to be sequence equal to:
             {must.Separator}
             {must.NormalizeExpressionIndentation(expectedExpression).Indent()}
             {must.Separator}
             But it was not.
             {must.Separator}
             Diff:
             {must.Separator}
             {DiffGenerator.CreateDiff(expected: expectedJson, actual: actualJson)}
             {must.Separator}
             Actual was:
             {must.Separator}
             {actualJson}
             {must.Separator}
             Expected was:
             {must.Separator}
             {expectedJson}
             {must.Separator}
             """);
   }
}
