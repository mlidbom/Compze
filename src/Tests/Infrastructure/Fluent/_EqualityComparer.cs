using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE.ReflectionCE;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectEqualityAssertions
{
   public static Must<TValue> Be<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      if(expected is null && must.Actual is null)
         return must;

      return must.Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode(expected, expectedExpression);
   }

   public static Must<TValue> Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      must.Satisfy(it => Equals(it, expected), messageOverride: BuildFailureMessage);
      must.Satisfy(it => Equals(expected, it), messageOverride: BuildFailureMessage);

      must.Satisfy(it => (it as IEquatable<TValue>)?.Equals(expected) ?? true, messageOverride: BuildFailureMessage);
      must.Satisfy(it => (expected as IEquatable<TValue>)?.Equals(it) ?? true, messageOverride: BuildFailureMessage);

      must.Satisfy(it => it.DeclaredType().Operators.Equality?.Invoke(it, expected) ?? true, failureMessage: it => "it == expected should have returned true", messageOverride: BuildFailureMessage);
      must.Satisfy(it => it.DeclaredType().Operators.Equality?.Invoke(expected, it) ?? true, failureMessage: it => "expected == it should have returned true", messageOverride: BuildFailureMessage);

      must.Satisfy(it => !it.DeclaredType().Operators.InEquality?.Invoke(it, expected) ?? true, failureMessage: it => "it != expected should have returned false", messageOverride: BuildFailureMessage);
      must.Satisfy(it => !it.DeclaredType().Operators.InEquality?.Invoke(expected, it) ?? true, failureMessage: it => "expected != it should have returned false", messageOverride: BuildFailureMessage);

      must.Satisfy(it => (it as IComparable<TValue>)?.CompareTo(expected).Equals(0) ?? true, failureMessage: it => "it.CompareTo(expected) (IComparable<T>) should have returned 0", messageOverride: BuildFailureMessage);
      must.Satisfy(it => (expected as IComparable<TValue>)?.CompareTo(it).Equals(0) ?? true, failureMessage: it => "expected.CompareTo(it) (IComparable<T>) should have returned 0", messageOverride: BuildFailureMessage);

      must.Satisfy(it => (it as IComparable)?.CompareTo(expected).Equals(0) ?? true, failureMessage: it => "it.CompareTo(expected) (IComparable) should have returned 0", messageOverride: BuildFailureMessage);
      must.Satisfy(it => (expected as IComparable)?.CompareTo(it).Equals(0) ?? true, failureMessage: it => "expected.CompareTo(it) (IComparable) should have returned 0", messageOverride: BuildFailureMessage);

      // IStructuralEquatable - used for structural equality (e.g., arrays, tuples)
      must.Satisfy(it => (it as IStructuralEquatable)?.Equals(expected, StructuralComparisons.StructuralEqualityComparer) ?? true, failureMessage: it => "it.Equals(expected, StructuralEqualityComparer) should have returned true", messageOverride: BuildFailureMessage);
      must.Satisfy(it => (expected as IStructuralEquatable)?.Equals(it, StructuralComparisons.StructuralEqualityComparer) ?? true, failureMessage: it => "expected.Equals(it, StructuralEqualityComparer) should have returned true", messageOverride: BuildFailureMessage);

      // IStructuralComparable - used for structural comparison (e.g., arrays, tuples)
      must.Satisfy(it => (it as IStructuralComparable)?.CompareTo(expected, StructuralComparisons.StructuralComparer).Equals(0) ?? true, failureMessage: it => "it.CompareTo(expected, StructuralComparer) should have returned 0", messageOverride: BuildFailureMessage);
      must.Satisfy(it => (expected as IStructuralComparable)?.CompareTo(it, StructuralComparisons.StructuralComparer).Equals(0) ?? true, failureMessage: it => "expected.CompareTo(it, StructuralComparer) should have returned 0", messageOverride: BuildFailureMessage);

      must.Satisfy(it => !it.DeclaredType().Operators.LessThan?.Invoke(it, expected) ?? true, failureMessage: it => "it < expected should have returned false", messageOverride: BuildFailureMessage);
      must.Satisfy(it => !it.DeclaredType().Operators.LessThan?.Invoke(expected, it) ?? true, failureMessage: it => "expected < it should have returned false", messageOverride: BuildFailureMessage);

      must.Satisfy(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: it => "it <= expected should have returned true", messageOverride: BuildFailureMessage);
      must.Satisfy(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: it => "expected <= it should have returned true", messageOverride: BuildFailureMessage);

      must.Satisfy(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(expected, it) ?? true, failureMessage: it => "expected > it should have returned false", messageOverride: BuildFailureMessage);
      must.Satisfy(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(it, expected) ?? true, failureMessage: it => "it > expected should have returned false", messageOverride: BuildFailureMessage);

      must.Satisfy(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: it => "expected >= it should have returned true", messageOverride: BuildFailureMessage);
      must.Satisfy(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: it => "it >= expected should have returned true", messageOverride: BuildFailureMessage);

      must.Satisfy(it => it!.GetHashCode() == expected!.GetHashCode(), messageOverride: BuildFailureMessage);

      return must;

      string BuildFailureMessage(SatisfyCallInfo<TValue> info)
      {
         var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
         var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);
         return $"""
                 {must.Separator}
                 expected the object "it" returned by the expression: 
                 {must.Expression.Indent()}
                 to be equal to the the object "expected" returned by the expression:
                 {must.NormalizeExpressionIndentation(expectedExpression).Indent()}
                 but it failed the test: 
                 {info.PredicateExpression.Indent()}{FailureMessage()}
                 {must.Separator}
                 Diff:
                 {must.Separator}
                 {DiffGenerator.CreateDiff(expectedJson, actualJson)}
                 {must.Separator}
                 it was:
                 {must.Separator}
                 {actualJson}
                 {must.Separator}
                 expected was:
                 {must.Separator}
                 {expectedJson}
                 {must.Separator}
                 """;

         string FailureMessage() =>
            info.FailureMessage != null
               ? $""""

                  with the message: {info.FailureMessage?.Invoke(must.Actual)}""" 
                  """"
               : "";
      }
   }
}
