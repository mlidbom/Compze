using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_comparing_custom_types_with_NotBe : UniversalTestBase
{
   public class given_two_unequal_values_with_all_methods_working : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void NotBe_succeeds() => _actual.Must().NotBe(_unexpected);
   }

   public class given_two_equal_values_with_all_methods_working : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(42);

      [XF] public void NotBe_throws() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>();
   }

   public class given_two_unequal_values_but_Object_Equals_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.ObjectEquals);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void NotBe_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => !Equals(it, unexpected)");
   }

   public class given_two_unequal_values_but_Object_Equals_reversed_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.ObjectEquals);

      [XF] public void NotBe_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => !Equals(unexpected, it)");
   }

   public class given_two_unequal_values_but_IEquatable_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IEquatable);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void NotBe_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => !((it as IEquatable<TValue>)?.Equals(unexpected) ?? false)");
   }

   public class given_two_unequal_values_but_IEquatable_reversed_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.IEquatable);

      [XF] public void NotBe_throws_with_correct_predicate() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it => !((unexpected as IEquatable<TValue>)?.Equals(it) ?? false)");
   }

   public class given_two_unequal_values_but_operator_equality_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorEquality);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void NotBe_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!(it.DeclaredType().Operators.Equality?.Invoke(it, unexpected) ?? false)");
         message.Must().Contain("it == unexpected should have returned false");
      }
   }

   public class given_two_unequal_values_but_operator_equality_reversed_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.OperatorEquality);

      [XF] public void NotBe_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("!(it.DeclaredType().Operators.Equality?.Invoke(unexpected, it) ?? false)");
         message.Must().Contain("unexpected == it should have returned false");
      }
   }

   public class given_two_unequal_values_but_operator_inequality_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorInequality);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void NotBe_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.InEquality?.Invoke(it, unexpected) ?? true");
         message.Must().Contain("it != unexpected should have returned true");
      }
   }

   public class given_two_unequal_values_but_operator_inequality_reversed_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.OperatorInequality);

      [XF] public void NotBe_throws_with_correct_predicate_and_message()
      {
         var message = Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message;

         message.Must().Contain("it.DeclaredType().Operators.InEquality?.Invoke(unexpected, it) ?? true");
         message.Must().Contain("unexpected != it should have returned true");
      }
   }

   // Note: We do NOT test IComparable/IComparable<T>.CompareTo() because: returning 0 doesn't necessarily mean equality.
   // A type can implement CompareTo based on one property (e.g., LastName for sorting) while Equals compares multiple properties.
   //
   // Note: We do NOT test IStructuralComparable.CompareTo() because: returning 0 doesn't necessarily mean structural equality.
   //
   // Note: We do NOT test comparison operators (<, >, <=, >=) because: these define ordering relationships, not equality.
   // For types with partial orderings or custom comparison semantics, comparison results don't reliably indicate equality.

   // Note: No test for GetHashCode - equal hash codes for unequal objects is allowed by the contract

   public class given_two_unequal_values_but_IStructuralEquatable_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IStructuralEquatable);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void NotBe_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("it.Equals(unexpected, StructuralEqualityComparer) should have returned false");
   }

   public class given_two_unequal_values_but_IStructuralEquatable_reversed_is_broken : When_comparing_custom_types_with_NotBe
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.IStructuralEquatable);

      [XF] public void NotBe_throws_with_correct_predicate_and_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
            .Must().Throw<AssertionFailedException>()
            .Which.Message.Must().Contain("unexpected.Equals(it, StructuralEqualityComparer) should have returned false");
   }
}
