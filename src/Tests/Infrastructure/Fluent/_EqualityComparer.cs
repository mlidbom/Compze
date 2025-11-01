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
   public static Must<TValue> Be<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => must.Satisfy(it => Equals(it, expected),
                      messageOverride: () =>
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

   public static Must<TValue> Be_transitively_equal_to_according_to_every_supported_comparison_method_and_hashcode<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {
      var usedArguments = new List<AssertionArgumentInfo>
                          {
                             new("actual", must.Expression, must.Actual)
                          };

      must.Satisfy(it => it != null && expected != null);

      must.Satisfy(it => Equals(it, expected), usedArguments: usedArguments);
      must.Satisfy(it => Equals(expected, it), usedArguments: usedArguments);

      must.Satisfy(it => (it as IEquatable<TValue>)?.Equals(expected) ?? true);
      must.Satisfy(it => (expected as IEquatable<TValue>)?.Equals(it) ?? true);

      must.Satisfy(it => it.DeclaredType().Operators.Equality?.Invoke(it, expected) ?? true, failureMessage: it => "Operator == should have returned true", usedArguments: usedArguments);
      must.Satisfy(it => it.DeclaredType().Operators.Equality?.Invoke(expected, it) ?? true, failureMessage: it => "Operator == should have returned true", usedArguments: usedArguments);

      must.Satisfy(it => !it.DeclaredType().Operators.InEquality?.Invoke(it, expected) ?? true, failureMessage: it => "Operator != should have returned false", usedArguments: usedArguments);
      must.Satisfy(it => !it.DeclaredType().Operators.InEquality?.Invoke(expected, it) ?? true, failureMessage: it => "Operator != should have returned false", usedArguments: usedArguments);

      must.Satisfy(it => EqualityComparer<TValue>.Default.Equals(it, expected), failureMessage: it => "default equality comparer should have returned true", usedArguments: usedArguments);
      must.Satisfy(it => EqualityComparer<TValue>.Default.Equals(expected, it), failureMessage: it => "default equality comparer should have returned true", usedArguments: usedArguments);

      must.Satisfy(it => (it as IComparable<TValue>)?.CompareTo(expected).Equals(0) ?? true, failureMessage: it => "IComparable<T>.CompareTo should have returned 0", usedArguments: usedArguments);
      must.Satisfy(it => (expected as IComparable<TValue>)?.CompareTo(it).Equals(0) ?? true, failureMessage: it => "IComparable<T>.CompareTo should have returned 0", usedArguments: usedArguments);

      must.Satisfy(it => (it as IComparable)?.CompareTo(expected).Equals(0) ?? true, failureMessage: it => "IComparable.CompareTo should have returned 0", usedArguments: usedArguments);
      must.Satisfy(it => (expected as IComparable)?.CompareTo(it).Equals(0) ?? true, failureMessage: it => "IComparable.CompareTo should have returned 0", usedArguments: usedArguments);

      must.Satisfy(it => Comparer<TValue>.Default.Compare(it, expected) == 0, failureMessage: it => "Default comparer should have returned 0", usedArguments: usedArguments);
      must.Satisfy(it => Comparer<TValue>.Default.Compare(expected, it) == 0, failureMessage: it => "Default comparer should have returned 0", usedArguments: usedArguments);

      must.Satisfy(it => !it.DeclaredType().Operators.LessThan?.Invoke(expected, it) ?? true, failureMessage: it => "Operator < should have returned false", usedArguments: usedArguments);
      must.Satisfy(it => !it.DeclaredType().Operators.LessThan?.Invoke(it, expected) ?? true, failureMessage: it => "Operator < should have returned false", usedArguments: usedArguments);

      must.Satisfy(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: it => "Operator <= should have returned true", usedArguments: usedArguments);
      must.Satisfy(it => it.DeclaredType().Operators.LessThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: it => "Operator <= should have returned true", usedArguments: usedArguments);

      must.Satisfy(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(expected, it) ?? true, failureMessage: it => "Operator > should have returned false", usedArguments: usedArguments);
      must.Satisfy(it => !it.DeclaredType().Operators.GreaterThan?.Invoke(it, expected) ?? true, failureMessage: it => "Operator > should have returned false", usedArguments: usedArguments);

      must.Satisfy(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(expected, it) ?? true, failureMessage: it => "Operator >= should have returned true", usedArguments: usedArguments);
      must.Satisfy(it => it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(it, expected) ?? true, failureMessage: it => "Operator >= should have returned true", usedArguments: usedArguments);

      must.Satisfy(it => it!.GetHashCode() == expected!.GetHashCode(), usedArguments: usedArguments);

      return must;
   }
}
