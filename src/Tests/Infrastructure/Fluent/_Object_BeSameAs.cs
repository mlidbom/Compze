using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectReferenceEqualityAssertions
{
   public static Must<TValue> BeSameAs<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      where TValue : class
   {
      must.Satisfy(it => ReferenceEquals(it, expected),
                   messageOverride: info => $"""
                                            {must.Separator}
                                            expected the object "it" returned by the expression: 
                                            {must.Separator}
                                            {must.Expression.Indent()}
                                            {must.Separator}
                                            to be the same reference as the object "expected" returned by the expression:
                                            {must.Separator}
                                            {must.NormalizeExpressionIndentation(expectedExpression).Indent()}
                                            {must.Separator}
                                            but they are different objects (reference equality failed)
                                            {must.Separator}
                                            """);
      return must;
   }

   public static Must<TValue> NotBeSameAs<TValue>(this Must<TValue> must, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
      where TValue : class
   {
      must.Satisfy(it => !ReferenceEquals(it, unexpected),
                   messageOverride: info => $"""
                                            {must.Separator}
                                            expected the object "it" returned by the expression: 
                                            {must.Separator}
                                            {must.Expression.Indent()}
                                            {must.Separator}
                                            to not be the same reference as the object "unexpected" returned by the expression:
                                            {must.Separator}
                                            {must.NormalizeExpressionIndentation(unexpectedExpression).Indent()}
                                            {must.Separator}
                                            but they reference the same object (reference equality succeeded when it shouldn't)
                                            {must.Separator}
                                            """);
      return must;
   }
}
