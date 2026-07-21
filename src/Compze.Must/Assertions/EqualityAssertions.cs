using System.Collections;
using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Must._private.Serialization;
using Newtonsoft.Json;
using Compze.Must._private;

namespace Compze.Must;

// ReSharper disable InconsistentNaming
/// <summary>Value-equality assertions, checked consistently across every comparison mechanism the type supports.</summary>
public static class EqualityAssertions
{
   /// <summary>Asserts that the value equals <paramref name="expected"/> consistently across <see cref="object.Equals(object)"/>, <see cref="System.IEquatable{T}"/>, equality and comparison operators, <see cref="System.IComparable"/>, structural equality, and <see cref="object.GetHashCode"/>.</summary>
   public static IAssertionContext<TValue> Be<TValue, TExpected>(this IAssertionContext<TValue> context, TExpected expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!) =>
      context.Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode(expected, expectedExpression);

   static IAssertionContext<TValue> Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode<TValue, TExpected>(this IAssertionContext<TValue> context, TExpected expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      if(expected is TValue expectedAsActual)
      {
         return context.Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal(expectedAsActual, expectedExpression);
      }

      if(context.Actual is TExpected)
      {
         return context.Cast<TExpected>().Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal(expected, expectedExpression).Cast<TValue>();
      }

      return context.Cast<object>()
                    .Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal(expected!, expectedExpression)
                    .Cast<TValue>();
   }

   static IAssertionContext<TValue> Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode_internal<TValue>(this IAssertionContext<TValue> context, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      if(expected is null && context.Actual is null)
         return context;

      context.RunAssertion(it => Equals(it, expected), messageOverride: BuildFailureMessage);
      context.RunAssertion(it => Equals(expected, it), messageOverride: BuildFailureMessage);

      context.RunAssertion(it => (it as IEquatable<TValue>)?.Equals(expected) ?? true, messageOverride: BuildFailureMessage);
      context.RunAssertion(it => (expected as IEquatable<TValue>)?.Equals(it) ?? true, messageOverride: BuildFailureMessage);

      context.RunAssertion(it => it.DeclaredType().Operators.Equality?.Invoke(it, expected) ?? true, failureMessage: _ => "it == expected should have returned true", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => it.DeclaredType().Operators.Equality?.Invoke(expected, it) ?? true, failureMessage: _ => "expected == it should have returned true", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => !it.DeclaredType().Operators.InEquality?.Invoke(it, expected) ?? true, failureMessage: _ => "it != expected should have returned false", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !it.DeclaredType().Operators.InEquality?.Invoke(expected, it) ?? true, failureMessage: _ => "expected != it should have returned false", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => (it as IStructuralEquatable)?.Equals(expected, StructuralComparisons.StructuralEqualityComparer) ?? true, failureMessage: _ => "it.Equals(expected, StructuralEqualityComparer) should have returned true", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => (expected as IStructuralEquatable)?.Equals(it, StructuralComparisons.StructuralEqualityComparer) ?? true, failureMessage: _ => "expected.Equals(it, StructuralEqualityComparer) should have returned true", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => (it as IComparable<TValue>)?.CompareTo(expected).Equals(0) ?? true, failureMessage: _ => "it.CompareTo(expected) (IComparable<T>) should have returned 0", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => (expected as IComparable<TValue>)?.CompareTo(it).Equals(0) ?? true, failureMessage: _ => "expected.CompareTo(it) (IComparable<T>) should have returned 0", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => (it as IComparable)?.CompareTo(expected).Equals(0) ?? true, failureMessage: _ => "it.CompareTo(expected) (IComparable) should have returned 0", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => (expected as IComparable)?.CompareTo(it).Equals(0) ?? true, failureMessage: _ => "expected.CompareTo(it) (IComparable) should have returned 0", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => (it as IStructuralComparable)?.CompareTo(expected, StructuralComparisons.StructuralComparer).Equals(0) ?? true, failureMessage: _ => "it.CompareTo(expected, StructuralComparer) should have returned 0", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => (expected as IStructuralComparable)?.CompareTo(it, StructuralComparisons.StructuralComparer).Equals(0) ?? true, failureMessage: _ => "expected.CompareTo(it, StructuralComparer) should have returned 0", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => !it.DeclaredType().Operators.LessThan?.Invoke(it, expected) ?? true, failureMessage: _ => "it < expected should have returned false", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !it.DeclaredType().Operators.LessThan?.Invoke(expected, it) ?? true, failureMessage: _ => "expected < it should have returned false", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: _ => "it <= expected should have returned true", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: _ => "expected <= it should have returned true", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(expected, it) ?? true, failureMessage: _ => "expected > it should have returned false", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(it, expected) ?? true, failureMessage: _ => "it > expected should have returned false", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: _ => "expected >= it should have returned true", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: _ => "it >= expected should have returned true", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => it!.GetHashCode() == expected!.GetHashCode(), messageOverride: BuildFailureMessage);

      return context;

      string BuildFailureMessage(AssertionCallInfo<TValue> info)
      {
         var actualJson = JsonConvert.SerializeObject(context.Actual, TestingJsonSettings.AllMembers);
         var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);

         var actualToString = context.Actual?.ToString();
         var expectedToString = expected?.ToString();

         var skipDiff = actualToString == actualJson && expectedToString == expectedJson;

         var diffMessage = skipDiff
                              ? $"Expected {expectedToString} but got {actualToString}"
                              : $"""
                                 Diff:
                                 {AssertionContext.Separator}
                                 {DiffGenerator.CreateDiff(expectedJson, actualJson)}
                                 """;

         return $"""
                 {context.FailingAssertionHeading(nameof(Be), expectedExpression)}
                 {diffMessage}
                 {AssertionContext.Separator}
                 {context.ExpressionValue()}
                 {context.ExpressionValue(expectedExpression, expected)}
                 the first failing equivalency test was: 
                 {info.PredicateExpression.Indent()}{FailureMessage()}
                 {AssertionContext.Separator}
                 """;

         string FailureMessage() =>
            info.FailureMessage != null
               ? $""""

                  with the message: {info.FailureMessage?.Invoke(context.Actual)}""" 
                  """"
               : "";
      }
   }

   /// <summary>Asserts that the value is not equal to <paramref name="unexpected"/> by any supported comparison mechanism.</summary>
   public static IAssertionContext<TValue> NotBe<TValue, TUnExpected>(this IAssertionContext<TValue> context, TUnExpected unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!) =>
      context.Not_be_equal_to_according_to_any_supported_comparison_method_in_any_direction(unexpected, unexpectedExpression);

   static IAssertionContext<TValue> Not_be_equal_to_according_to_any_supported_comparison_method_in_any_direction<TValue, TUnExpected>(this IAssertionContext<TValue> context, TUnExpected unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
   {
      if(unexpected is TValue unExpectedAsActual)
      {
         return context.Not_be_equal_to_according_to_any_supported_comparison_method_in_any_direction_internal(unExpectedAsActual, unexpectedExpression);
      }

      if(context.Actual is TUnExpected)
      {
         return context.Cast<TUnExpected>().Not_be_equal_to_according_to_any_supported_comparison_method_in_any_direction_internal(unexpected, unexpectedExpression)
                       .Cast<TValue>();
      }

      return context.Cast<object>()
                    .Not_be_equal_to_according_to_any_supported_comparison_method_in_any_direction_internal(unexpected!, unexpectedExpression)
                    .Cast<TValue>();
   }

   static IAssertionContext<TValue> Not_be_equal_to_according_to_any_supported_comparison_method_in_any_direction_internal<TValue>(this IAssertionContext<TValue> context, TValue unexpected, [CallerArgumentExpression(nameof(unexpected))] string unexpectedExpression = null!)
   {
      if(unexpected is null && context.Actual is null)
         throw new AssertionFailedException("Both values are null, so they are equal");

      if(unexpected is null || context.Actual is null)
         return context; // One is null, the other isn't, so they're not equal

      // Equality checks - these are the only reliable indicators that objects are NOT equal
      context.RunAssertion(it => !Equals(it, unexpected), messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !Equals(unexpected, it), messageOverride: BuildFailureMessage);

      context.RunAssertion(it => !((it as IEquatable<TValue>)?.Equals(unexpected) ?? false), messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !((unexpected as IEquatable<TValue>)?.Equals(it) ?? false), messageOverride: BuildFailureMessage);

      context.RunAssertion(it => !(it.DeclaredType().Operators.Equality?.Invoke(it, unexpected) ?? false), failureMessage: _ => "it == unexpected should have returned false", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !(it.DeclaredType().Operators.Equality?.Invoke(unexpected, it) ?? false), failureMessage: _ => "unexpected == it should have returned false", messageOverride: BuildFailureMessage);

      context.RunAssertion(it => it.DeclaredType().Operators.InEquality?.Invoke(it, unexpected) ?? true, failureMessage: _ => "it != unexpected should have returned true", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => it.DeclaredType().Operators.InEquality?.Invoke(unexpected, it) ?? true, failureMessage: _ => "unexpected != it should have returned true", messageOverride: BuildFailureMessage);

      // IStructuralEquatable - used for structural equality (e.g., arrays, tuples)
      context.RunAssertion(it => !((it as IStructuralEquatable)?.Equals(unexpected, StructuralComparisons.StructuralEqualityComparer) ?? false), failureMessage: _ => "it.Equals(unexpected, StructuralEqualityComparer) should have returned false", messageOverride: BuildFailureMessage);
      context.RunAssertion(it => !((unexpected as IStructuralEquatable)?.Equals(it, StructuralComparisons.StructuralEqualityComparer) ?? false), failureMessage: _ => "unexpected.Equals(it, StructuralEqualityComparer) should have returned false", messageOverride: BuildFailureMessage);

      // Unlike in the Be_ version we do not check any of the below, because none of them have any return values that are guaranteed to be wrong for non-equal objects:
      // - GetHashCode(), IComparable/IComparable<T>, IStructuralComparable, Comparison operators (<, >, <=, >=)

      return context;

      string BuildFailureMessage(AssertionCallInfo<TValue> info)
      {
         return $"""
                 {context.FailingAssertionHeading(nameof(NotBe), unexpectedExpression)}
                 first failing test: 
                 {info.PredicateExpression.Indent()}{FailureMessage()}
                 {AssertionContext.Separator}
                 {context.Diff(unexpected, context.Actual, "unexpected", "actual")}
                 {context.ExpressionValue()}
                 {context.ExpressionValue(unexpectedExpression, unexpected)}
                 """;

         string FailureMessage() =>
            info.FailureMessage != null
               ? $""""

                  with the message: {info.FailureMessage?.Invoke(context.Actual)}""" 
                  """"
               : "";
      }
   }
}
