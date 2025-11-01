using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE.ReflectionCE;
using DiffPlex.Renderer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectEqualityAssertions
{
   public static Must<TValue>? Be<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => must.Satisfy(it => Equals(it, expected),
                      () =>
                      {
                         var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
                         var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);
                         return $"""
                                 {must.Separator}
                                 expected the object returned by the expression: 
                                 {must.Separator}
                                 {must.Expression}
                                 {must.Separator}
                                 to be the equal to the the object returned by the expression:
                                 {must.Separator}
                                 {must.NormalizeExpressionIndentation(expectedExpression)}
                                 {must.Separator}
                                 but it was not and a diff of the instances is:
                                 {must.Separator}
                                 {UnidiffRenderer.GenerateUnidiff(oldText: expectedJson, newText: actualJson, oldFileName: "expected", newFileName: "actual")}
                                 {must.Separator}
                                 Actual was:
                                 {must.Separator}
                                 {actualJson}
                                 {must.Separator}
                                 Expected was:
                                 {must.Separator}
                                 {expectedJson}
                                 {must.Separator}
                                 """;
                      });

   public static Must<TValue>? Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      must.Satisfy(actual => Equals(actual, expected));
      must.Satisfy(actual => Equals(expected, actual));

      must.Satisfy(actual => (actual as IEquatable<TValue>)?.Equals(expected) ?? true);
      must.Satisfy(actual => (expected as IEquatable<TValue>)?.Equals(actual) ?? true);

      must.Satisfy(actual => actual.DeclaredType().Operators.Equality?.Invoke(actual, expected) ?? true, () => "Operator == should have returned true");
      must.Satisfy(actual => actual.DeclaredType().Operators.Equality?.Invoke(expected, actual) ?? true, () => "Operator == should have returned true");

      must.Satisfy(actual => !actual.DeclaredType().Operators.InEquality?.Invoke(actual, expected) ?? true, () => "Operator != should have returned false");
      must.Satisfy(actual => !actual.DeclaredType().Operators.InEquality?.Invoke(expected, actual) ?? true, () => "Operator != should have returned false");

      must.Satisfy(actual => EqualityComparer<TValue>.Default.Equals(actual, expected), () => "default equality comparer should have returned true");
      must.Satisfy(actual => EqualityComparer<TValue>.Default.Equals(expected, actual), () => "default equality comparer should have returned true");

      must.Satisfy(actual => (actual as IComparable<TValue>)?.CompareTo(expected).Equals(0) ?? true, () => "IComparable<T>.CompareTo should have returned 0");
      must.Satisfy(actual => (expected as IComparable<TValue>)?.CompareTo(actual).Equals(0) ?? true, () => "IComparable<T>.CompareTo should have returned 0");

      must.Satisfy(actual => (actual as IComparable)?.CompareTo(expected).Equals(0) ?? true, () => "IComparable.CompareTo should have returned 0");
      must.Satisfy(actual => (expected as IComparable)?.CompareTo(actual).Equals(0) ?? true, () => "IComparable.CompareTo should have returned 0");

      must.Satisfy(actual => Comparer<TValue>.Default.Compare(actual, expected) == 0, () => "Default comparer should have returned 0");
      must.Satisfy(actual => Comparer<TValue>.Default.Compare(expected, actual) == 0, () => "Default comparer should have returned 0");

      must.Satisfy(actual => !actual.DeclaredType().Operators.LessThan?.Invoke(expected, actual) ?? true, () => "Operator < should have returned false");
      must.Satisfy(actual => !actual.DeclaredType().Operators.LessThan?.Invoke(actual, expected) ?? true, () => "Operator < should have returned false");

      must.Satisfy(actual => actual.DeclaredType().Operators.LessThanOrEqual?.Invoke(expected, actual) ?? true, () => "Operator <= should have returned true");
      must.Satisfy(actual => actual.DeclaredType().Operators.LessThanOrEqual?.Invoke(actual, expected) ?? true, () => "Operator <= should have returned true");

      must.Satisfy(actual => !actual.DeclaredType().Operators.GreaterThan?.Invoke(expected, actual) ?? true, () => "Operator > should have returned false");
      must.Satisfy(actual => !actual.DeclaredType().Operators.GreaterThan?.Invoke(actual, expected) ?? true, () => "Operator > should have returned false");

      must.Satisfy(actual => actual.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(expected, actual) ?? true, () => "Operator >= should have returned true");
      must.Satisfy(actual => actual.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(actual, expected) ?? true, () => "Operator >= should have returned true");

      must.Satisfy(actual => actual!.GetHashCode() == expected!.GetHashCode());

      return must;
   }
}
