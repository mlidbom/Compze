using AssertionFailedException = Compze.Must.AssertionFailedException;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Must.Specifications;

public class When_calling_Must_Be_with_custom_types : UniversalTestBase
{
   public class given_two_equal_values_with_all_methods_working : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_succeeds() => _actual.Must().Be(_expected);
   }

   public class given_two_equal_values_but_Object_Equals_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.ObjectEquals);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => Equals(it, expected)");
   }

   public class given_two_equal_values_but_Object_Equals_reversed_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.ObjectEquals);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => Equals(expected, it)");
   }

   public class given_two_equal_values_but_IEquatable_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IEquatable);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => (it as IEquatable<TValue>)?.Equals(expected) ?? true");
   }

   public class given_two_equal_values_but_IEquatable_reversed_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.IEquatable);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => (expected as IEquatable<TValue>)?.Equals(it) ?? true");
   }

   public class given_two_equal_values_but_operator_equality_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorEquality);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message
           .Must()
           .Contain("it.DeclaredType().Operators.Equality?.Invoke(it, expected) ?? true")
           .Contain("it == expected should have returned true");
   }

   public class given_two_equal_values_but_operator_equality_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_inequality_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_inequality_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_IComparable_generic_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_IComparable_generic_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_IComparable_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_IComparable_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_less_than_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_less_than_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_less_than_or_equal_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_less_than_or_equal_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_greater_than_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_greater_than_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_greater_than_or_equal_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_operator_greater_than_or_equal_reversed_is_broken : When_calling_Must_Be_with_custom_types
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

   public class given_two_equal_values_but_GetHashCode_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.GetHashCode);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => it!.GetHashCode() == expected!.GetHashCode()");
   }

   public class given_two_equal_values_but_IStructuralEquatable_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IStructuralEquatable);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it.Equals(expected, StructuralEqualityComparer) should have returned true");
   }

   public class given_two_equal_values_but_IStructuralEquatable_reversed_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.IStructuralEquatable);

      [XF] public void Be_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("expected.Equals(it, StructuralEqualityComparer) should have returned true");
   }

   public class given_two_equal_values_but_IStructuralComparable_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IStructuralComparable);
      readonly ComparableWithErrorInjectionSupport _expected = new(42);

      [XF] public void Be_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it.CompareTo(expected, StructuralComparer) should have returned 0");
   }

   public class given_two_equal_values_but_IStructuralComparable_reversed_is_broken : When_calling_Must_Be_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _expected = new(42, BreakComparableMethod.IStructuralComparable);

      [XF] public void Be_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().Be(_expected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("expected.CompareTo(it, StructuralComparer) should have returned 0");
   }
}
