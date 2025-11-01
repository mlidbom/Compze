using Compze.Tests.Infrastructure.Fluent.Serialization;
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
      var actual = must.Actual;

      // IEquatable<T>.Equals - both directions
      if(actual is IEquatable<TValue> equatable)
      {
         must.Satisfy(it => equatable.Equals(expected));
      }
      if(expected is IEquatable<TValue> expectedEquatable)
      {
         must.Satisfy(it => expectedEquatable.Equals(actual));
      }

      // Object.Equals - both directions
      must.Satisfy(it => Equals(it, expected));
      must.Satisfy(it => Equals(expected, it));

      // == operator - both directions (using reflection to call actual operator if it exists)
      var equalityOperator = typeof(TValue).GetMethod("op_Equality", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, [typeof(TValue), typeof(TValue)], null);
      if(equalityOperator != null)
      {
         must.Satisfy(it => (bool)equalityOperator.Invoke(null, [it, expected])!);
         must.Satisfy(it => (bool)equalityOperator.Invoke(null, [expected, it])!);
      }
      else
      {
         // Fallback to EqualityComparer if no operator is defined
         must.Satisfy(actual => EqualityComparer<TValue>.Default.Equals(actual, expected));
         must.Satisfy(actual => EqualityComparer<TValue>.Default.Equals(expected, actual));
      }

      // != operator - both directions (should return false)
      var inequalityOperator = typeof(TValue).GetMethod("op_Inequality", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, [typeof(TValue), typeof(TValue)], null);
      if(inequalityOperator != null)
      {
         must.Satisfy(it => (bool)inequalityOperator.Invoke(null, [it, expected])! == false);
         must.Satisfy(it => (bool)inequalityOperator.Invoke(null, [expected, it])! == false);
      }
      else
      {
         // Fallback to EqualityComparer if no operator is defined
         must.Satisfy(it => !EqualityComparer<TValue>.Default.Equals(it, expected) == false);
         must.Satisfy(it => !EqualityComparer<TValue>.Default.Equals(expected, it) == false);
      }

      // IComparable<T>.CompareTo - both directions (should return 0)
      if(actual is IComparable<TValue> actualAsGenericComparable)
      {
         must.Satisfy(it => actualAsGenericComparable.CompareTo(expected) == 0);
      }
      if(expected is IComparable<TValue> expectedAsGenericComparable)
      {
         must.Satisfy(it => expectedAsGenericComparable.CompareTo(actual) == 0);
      }

      // IComparable.CompareTo - both directions (should return 0)
      if(actual is IComparable actualAsComparable)
      {
         must.Satisfy(it => actualAsComparable.CompareTo(expected) == 0);
      }
      if(expected is IComparable expectedAsComparable)
      {
         must.Satisfy(it => expectedAsComparable.CompareTo(actual) == 0);
      }

      // Comparer<T>.Default.Compare - both directions (should return 0)
      must.Satisfy(it => Comparer<TValue>.Default.Compare(it, expected) == 0);
      must.Satisfy(it => Comparer<TValue>.Default.Compare(expected, it) == 0);

      // GetHashCode - should return the same value for equal objects
      must.Satisfy(it => it!.GetHashCode() == expected!.GetHashCode());

      return must;
   }
}
