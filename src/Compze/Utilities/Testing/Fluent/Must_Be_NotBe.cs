using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Testing.Fluent.Serialization;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.Fluent;

// ReSharper disable InconsistentNaming
public static class Must_Be_NotBe
{
   public static IMust<TValue> Be<TValue, TExpected>(this IMust<TValue> must, TExpected expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      must.Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode(expected, expectedExpression);

   public static IMust<TValue> Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode<TValue, TExpected>(this IMust<TValue> must, TExpected expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      if(expected is TValue expectedAsActual)
      {
         return must.Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal(expectedAsActual, expectedExpression);
      }

      if(must.Actual is TExpected actualAsExpected)
      {
         must.Cast<TExpected>().Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal(expected, expectedExpression).Cast<TValue>();
      }

      return must.Cast<object>()
                 .Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal(expected!, expectedExpression)
                 .Cast<TValue>();
   }

   static IMust<TValue> Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      if(expected is null && must.Actual is null)
         return must;

      must.SatisfyInternal(it => Equals(it, expected), messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => Equals(expected, it), messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => (it as IEquatable<TValue>)?.Equals(expected) ?? true, messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => (expected as IEquatable<TValue>)?.Equals(it) ?? true, messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => it.DeclaredType().Operators.Equality?.Invoke(it, expected) ?? true, failureMessage: it => "it == expected should have returned true", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => it.DeclaredType().Operators.Equality?.Invoke(expected, it) ?? true, failureMessage: it => "expected == it should have returned true", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => !it.DeclaredType().Operators.InEquality?.Invoke(it, expected) ?? true, failureMessage: it => "it != expected should have returned false", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !it.DeclaredType().Operators.InEquality?.Invoke(expected, it) ?? true, failureMessage: it => "expected != it should have returned false", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => (it as IStructuralEquatable)?.Equals(expected, StructuralComparisons.StructuralEqualityComparer) ?? true, failureMessage: it => "it.Equals(expected, StructuralEqualityComparer) should have returned true", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => (expected as IStructuralEquatable)?.Equals(it, StructuralComparisons.StructuralEqualityComparer) ?? true, failureMessage: it => "expected.Equals(it, StructuralEqualityComparer) should have returned true", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => (it as IComparable<TValue>)?.CompareTo(expected).Equals(0) ?? true, failureMessage: it => "it.CompareTo(expected) (IComparable<T>) should have returned 0", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => (expected as IComparable<TValue>)?.CompareTo(it).Equals(0) ?? true, failureMessage: it => "expected.CompareTo(it) (IComparable<T>) should have returned 0", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => (it as IComparable)?.CompareTo(expected).Equals(0) ?? true, failureMessage: it => "it.CompareTo(expected) (IComparable) should have returned 0", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => (expected as IComparable)?.CompareTo(it).Equals(0) ?? true, failureMessage: it => "expected.CompareTo(it) (IComparable) should have returned 0", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => (it as IStructuralComparable)?.CompareTo(expected, StructuralComparisons.StructuralComparer).Equals(0) ?? true, failureMessage: it => "it.CompareTo(expected, StructuralComparer) should have returned 0", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => (expected as IStructuralComparable)?.CompareTo(it, StructuralComparisons.StructuralComparer).Equals(0) ?? true, failureMessage: it => "expected.CompareTo(it, StructuralComparer) should have returned 0", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => !it.DeclaredType().Operators.LessThan?.Invoke(it, expected) ?? true, failureMessage: it => "it < expected should have returned false", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !it.DeclaredType().Operators.LessThan?.Invoke(expected, it) ?? true, failureMessage: it => "expected < it should have returned false", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: it => "it <= expected should have returned true", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: it => "expected <= it should have returned true", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(expected, it) ?? true, failureMessage: it => "expected > it should have returned false", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(it, expected) ?? true, failureMessage: it => "it > expected should have returned false", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: it => "expected >= it should have returned true", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: it => "it >= expected should have returned true", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => it!.GetHashCode() == expected!.GetHashCode(), messageOverride: BuildFailureMessage);

      return must;

      string BuildFailureMessage(SatisfyCallInfo<TValue> info)
      {
         var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
         var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);
         return $"""
                 {must.FailingAssertionHeading("Be", expectedExpression)}
                 the fist failing equivalency test was: 
                 {info.PredicateExpression.Indent()}{FailureMessage()}
                 {must.Separator}
                 Diff:
                 {must.Separator}
                 {DiffGenerator.CreateDiff(expectedJson, actualJson)}
                 {must.Separator}
                 "it" was:
                 {must.Separator}
                 {actualJson}
                 {must.Separator}
                 "expected" was:
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

   public static IMust<TValue> NotBe<TValue, TUnExpected>(this IMust<TValue> must, TUnExpected unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!) =>
      must.Not_be_transitively_equal_to_according_to_any_supported_comparison_method(unexpected, unexpectedExpression);

   public static IMust<TValue> Not_be_transitively_equal_to_according_to_any_supported_comparison_method<TValue, TUnExpected>(this IMust<TValue> must, TUnExpected unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
   {
      if(unexpected is TValue unExpectedAsActual)
      {
         return must.Not_be_transitively_equal_to_according_to_any_supported_comparison_method_internal(unExpectedAsActual, unexpectedExpression);
      }

      if(must.Actual is TUnExpected)
      {
         must.Cast<TUnExpected>().Not_be_transitively_equal_to_according_to_any_supported_comparison_method_internal(unexpected, unexpectedExpression)
             .Cast<TValue>();
      }

      return must.Cast<object>()
                 .Not_be_transitively_equal_to_according_to_any_supported_comparison_method_internal(unexpected!, unexpectedExpression)
                 .Cast<TValue>();
   }

   public static IMust<TValue> Not_be_transitively_equal_to_according_to_any_supported_comparison_method_internal<TValue>(this IMust<TValue> must, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
   {
      if(unexpected is null && must.Actual is null)
         throw new AssertionFailedException($"Both values are null, so they are equal");

      if(unexpected is null || must.Actual is null)
         return must; // One is null, the other isn't, so they're not equal

      // Equality checks - these are the only reliable indicators that objects are NOT equal
      must.SatisfyInternal(it => !Equals(it, unexpected), messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !Equals(unexpected, it), messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => !((it as IEquatable<TValue>)?.Equals(unexpected) ?? false), messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !((unexpected as IEquatable<TValue>)?.Equals(it) ?? false), messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => !(it.DeclaredType().Operators.Equality?.Invoke(it, unexpected) ?? false), failureMessage: it => "it == unexpected should have returned false", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !(it.DeclaredType().Operators.Equality?.Invoke(unexpected, it) ?? false), failureMessage: it => "unexpected == it should have returned false", messageOverride: BuildFailureMessage);

      must.SatisfyInternal(it => it.DeclaredType().Operators.InEquality?.Invoke(it, unexpected) ?? true, failureMessage: it => "it != unexpected should have returned true", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => it.DeclaredType().Operators.InEquality?.Invoke(unexpected, it) ?? true, failureMessage: it => "unexpected != it should have returned true", messageOverride: BuildFailureMessage);

      // IStructuralEquatable - used for structural equality (e.g., arrays, tuples)
      must.SatisfyInternal(it => !((it as IStructuralEquatable)?.Equals(unexpected, StructuralComparisons.StructuralEqualityComparer) ?? false), failureMessage: it => "it.Equals(unexpected, StructuralEqualityComparer) should have returned false", messageOverride: BuildFailureMessage);
      must.SatisfyInternal(it => !((unexpected as IStructuralEquatable)?.Equals(it, StructuralComparisons.StructuralEqualityComparer) ?? false), failureMessage: it => "unexpected.Equals(it, StructuralEqualityComparer) should have returned false", messageOverride: BuildFailureMessage);

      // Unlike in the Be_ version we do not check any of the below, because none of them have any return values that are guaranteed to be wrong for non-equal objects:
      // - GetHashCode(), IComparable/IComparable<T>, IStructuralComparable, Comparison operators (<, >, <=, >=)

      return must;

      string BuildFailureMessage(SatisfyCallInfo<TValue> info)
      {
         var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
         var unexpectedJson = JsonConvert.SerializeObject(unexpected, TestingJsonSettings.AllMembers);
         return $"""
                 {must.Separator}
                 expected the object "it" returned by the expression: 
                 {must.Expression.Indent()}
                 to not be equal to the the object "unexpected" returned by the expression:
                 {must.NormalizeExpressionIndentation(unexpectedExpression).Indent()}
                 but it failed the test: 
                 {info.PredicateExpression.Indent()}{FailureMessage()}
                 {must.Separator}
                 Diff:
                 {must.Separator}
                 {DiffGenerator.CreateDiff(unexpectedJson, actualJson)}
                 {must.Separator}
                 it was:
                 {must.Separator}
                 {actualJson}
                 {must.Separator}
                 unexpected was:
                 {must.Separator}
                 {unexpectedJson}
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
