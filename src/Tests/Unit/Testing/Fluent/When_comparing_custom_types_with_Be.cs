using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using System;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_comparing_custom_types_with_Be : UniversalTestBase
{
   [Flags]
   enum BreakComparableMethod
   {
      None = 0,
      ObjectEquals = 1 << 0,
      ObjectEqualsReversed = 1 << 1,
      IEquatable = 1 << 2,
      IEquatableReversed = 1 << 3,
      OperatorEquality = 1 << 4,
      OperatorEqualityReversed = 1 << 5,
      OperatorInequality = 1 << 6,
      OperatorInequalityReversed = 1 << 7,
      EqualityComparer = 1 << 8,
      EqualityComparerReversed = 1 << 9,
      IComparableGeneric = 1 << 10,
      IComparableGenericReversed = 1 << 11,
      IComparable = 1 << 12,
      IComparableReversed = 1 << 13,
      OperatorLessThan = 1 << 14,
      OperatorLessThanReversed = 1 << 15,
      OperatorLessThanOrEqual = 1 << 16,
      OperatorLessThanOrEqualReversed = 1 << 17,
      OperatorGreaterThan = 1 << 18,
      OperatorGreaterThanReversed = 1 << 19,
      OperatorGreaterThanOrEqual = 1 << 20,
      OperatorGreaterThanOrEqualReversed = 1 << 21,
      GetHashCode = 1 << 22
   }

   class ComparableWithErrorInjectionSupport : IEquatable<ComparableWithErrorInjectionSupport>, IComparable<ComparableWithErrorInjectionSupport>, IComparable
   {
      readonly int _value;
      readonly BreakComparableMethod _breakComparableMethod;

      public ComparableWithErrorInjectionSupport(int value, BreakComparableMethod breakComparableMethod = BreakComparableMethod.None)
      {
         _value = value;
         _breakComparableMethod = breakComparableMethod;
      }

      public override bool Equals(object? obj)
      {
         if(obj is not ComparableWithErrorInjectionSupport other) return false;
         var result = _value == other._value;
         if(_breakComparableMethod.HasFlag(BreakComparableMethod.ObjectEquals)) result = !result;
         return result;
      }

      public bool Equals(ComparableWithErrorInjectionSupport? other)
      {
         if(other is null) return false;
         var result = _value == other._value;
         if(_breakComparableMethod.HasFlag(BreakComparableMethod.IEquatable)) result = !result;
         return result;
      }

      public override int GetHashCode()
      {
         var hash = _value.GetHashCode();
         if(_breakComparableMethod.HasFlag(BreakComparableMethod.GetHashCode)) hash = ~hash;
         return hash;
      }

      public int CompareTo(ComparableWithErrorInjectionSupport? other)
      {
         if(other is null) return 1;
         var result = _value.CompareTo(other._value);
         if(_breakComparableMethod.HasFlag(BreakComparableMethod.IComparableGeneric) && result == 0) result = 1;
         return result;
      }

      public int CompareTo(object? obj)
      {
         if(obj is not ComparableWithErrorInjectionSupport other) throw new ArgumentException("Object is not a TestValue");
         var result = _value.CompareTo(other._value);
         if(_breakComparableMethod.HasFlag(BreakComparableMethod.IComparable) && result == 0) result = 1;
         return result;
      }

      public static bool operator ==(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
      {
         if(ReferenceEquals(left, right)) return true;
         if(left is null || right is null) return false;
         var result = left._value == right._value;
         if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorEquality)) result = !result;
         return result;
      }

      public static bool operator !=(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
      {
         if(ReferenceEquals(left, right)) return false;
         if(left is null || right is null) return true;
         var result = left._value != right._value;
         if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorInequality)) result = !result;
         return result;
      }

      public static bool operator <(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
      {
         if(left is null || right is null) return false;
         var result = left._value < right._value;
         if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorLessThan)) result = !result;
         return result;
      }

      public static bool operator >(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
      {
         if(left is null || right is null) return false;
         var result = left._value > right._value;
         if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorGreaterThan)) result = !result;
         return result;
      }

      public static bool operator <=(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
      {
         if(left is null || right is null) return false;
         var result = left._value <= right._value;
         if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorLessThanOrEqual)) result = !result;
         return result;
      }

      public static bool operator >=(ComparableWithErrorInjectionSupport? left, ComparableWithErrorInjectionSupport? right)
      {
         if(left is null || right is null) return false;
         var result = left._value >= right._value;
         if(left._breakComparableMethod.HasFlag(BreakComparableMethod.OperatorGreaterThanOrEqual)) result = !result;
         return result;
      }
   }

   public class given_two_equal_values_with_all_methods_working : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_succeeds() => _actual.Must().Be(_expected);
   }

   public class given_two_equal_values_but_Object_Equals_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.ObjectEquals);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => Equals(it, expected)");
   }

   public class given_two_equal_values_but_Object_Equals_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.ObjectEquals);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => Equals(expected, it)");
   }

   public class given_two_equal_values_but_IEquatable_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IEquatable);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => (it as IEquatable<TValue>)?.Equals(expected) ?? true");
   }

   public class given_two_equal_values_but_IEquatable_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.IEquatable);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => (expected as IEquatable<TValue>)?.Equals(it) ?? true");
   }

   public class given_two_equal_values_but_operator_equality_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorEquality);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.Equality?.Invoke(it, expected) ?? true");
         message.Must().Contain("it == expected should have returned true");
      }
   }

   public class given_two_equal_values_but_operator_equality_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.OperatorEquality);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.Equality?.Invoke(expected, it) ?? true");
         message.Must().Contain("expected == it should have returned true");
      }
   }

   public class given_two_equal_values_but_operator_inequality_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorInequality);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!it.DeclaredType().Operators.InEquality?.Invoke(it, expected) ?? true");
         message.Must().Contain("it != expected should have returned false");
      }
   }

   public class given_two_equal_values_but_operator_inequality_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.OperatorInequality);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!it.DeclaredType().Operators.InEquality?.Invoke(expected, it) ?? true");
         message.Must().Contain("expected != it should have returned false");
      }
   }

   public class given_two_equal_values_but_IComparable_generic_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IComparableGeneric);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("(it as IComparable<TValue>)?.CompareTo(expected).Equals(0) ?? true");
         message.Must().Contain("it.CompareTo(expected) (IComparable<T>) should have returned 0");
      }
   }

   public class given_two_equal_values_but_IComparable_generic_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.IComparableGeneric);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("(expected as IComparable<TValue>)?.CompareTo(it).Equals(0) ?? true");
         message.Must().Contain("expected.CompareTo(it) (IComparable<T>) should have returned 0");
      }
   }

   public class given_two_equal_values_but_IComparable_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IComparable);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("(it as IComparable)?.CompareTo(expected).Equals(0) ?? true");
         message.Must().Contain("it.CompareTo(expected) (IComparable) should have returned 0");
      }
   }

   public class given_two_equal_values_but_IComparable_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.IComparable);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("(expected as IComparable)?.CompareTo(it).Equals(0) ?? true");
         message.Must().Contain("expected.CompareTo(it) (IComparable) should have returned 0");
      }
   }

   public class given_two_equal_values_but_operator_less_than_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorLessThan);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!it.DeclaredType().Operators.LessThan?.Invoke(it, expected) ?? true");
         message.Must().Contain("it < expected should have returned false");
      }
   }

   public class given_two_equal_values_but_operator_less_than_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.OperatorLessThan);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!it.DeclaredType().Operators.LessThan?.Invoke(expected, it) ?? true");
         message.Must().Contain("expected < it should have returned false");
      }
   }

   public class given_two_equal_values_but_operator_less_than_or_equal_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorLessThanOrEqual);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.LessThanOrEqual?.Invoke(it, expected) ?? true");
         message.Must().Contain("it <= expected should have returned true");
      }
   }

   public class given_two_equal_values_but_operator_less_than_or_equal_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.OperatorLessThanOrEqual);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.LessThanOrEqual?.Invoke(expected, it) ?? true");
         message.Must().Contain("expected <= it should have returned true");
      }
   }

   public class given_two_equal_values_but_operator_greater_than_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorGreaterThan);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!it.DeclaredType().Operators.GreaterThan?.Invoke(it, expected) ?? true");
         message.Must().Contain("it > expected should have returned false");
      }
   }

   public class given_two_equal_values_but_operator_greater_than_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.OperatorGreaterThan);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!it.DeclaredType().Operators.GreaterThan?.Invoke(expected, it) ?? true");
         message.Must().Contain("expected > it should have returned false");
      }
   }

   public class given_two_equal_values_but_operator_greater_than_or_equal_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorGreaterThanOrEqual);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(it, expected) ?? true");
         message.Must().Contain("it >= expected should have returned true");
      }
   }

   public class given_two_equal_values_but_operator_greater_than_or_equal_reversed_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.OperatorGreaterThanOrEqual);

      [XF] public void Be_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.GreaterThanOrEqual?.Invoke(expected, it) ?? true");
         message.Must().Contain("expected >= it should have returned true");
      }
   }

   public class given_two_equal_values_but_GetHashCode_is_broken : When_comparing_custom_types_with_Be
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.GetHashCode);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => it!.GetHashCode() == expected!.GetHashCode()");
   }
}
