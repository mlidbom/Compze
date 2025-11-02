using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;
using __Must = Compze.Utilities.Testing.Fluent.__Must;
using AssertionFailedException = Compze.Utilities.Testing.Fluent.AssertionFailedException;
using Must___String = Compze.Utilities.Testing.Fluent.Must___String;
using Must_Be_NotBe = Compze.Utilities.Testing.Fluent.Must_Be_NotBe;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_calling_Must_NotBe_with_custom_types : UniversalTestBase
{
   public class given_two_unequal_values_with_all_methods_working : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_does_not_throw() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected);
   }

   public class with_two_equal_values_with_all_methods_working : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(42);

      [XF] public void it_throws() =>
         Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
           .Must().Throw<AssertionFailedException>();
   }

   public class with_two_unequal_values_but_Object_Equals_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.ObjectEquals);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                          .Must().Throw<AssertionFailedException>()
                                          .Which.Message), "it => !Equals(it, unexpected)");
   }

   public class with_two_unequal_values_but_Object_Equals_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.ObjectEquals);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                          .Must().Throw<AssertionFailedException>()
                                          .Which.Message), "it => !Equals(unexpected, it)");
   }

   public class with_two_unequal_values_but_IEquatable_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IEquatable);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                          .Must().Throw<AssertionFailedException>()
                                          .Which.Message), "it => !((it as IEquatable<TValue>)?.Equals(unexpected) ?? false)");
   }

   public class with_two_unequal_values_but_IEquatable_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.IEquatable);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                          .Must().Throw<AssertionFailedException>()
                                          .Which.Message), "it => !((unexpected as IEquatable<TValue>)?.Equals(it) ?? false)");
   }

   public class with_two_unequal_values_but_operator_equality_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorEquality);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Must___String.Contain(Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                                                .Must().Throw<AssertionFailedException>()
                                                                .Which
                                                                .Message), "!(it.DeclaredType().Operators.Equality?.Invoke(it, unexpected) ?? false)"), "it == unexpected should have returned false");
   }

   public class given_two_unequal_values_but_operator_equality_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.OperatorEquality);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Must___String.Contain(Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                                                .Must().Throw<AssertionFailedException>()
                                                                .Which
                                                                .Message), "!(it.DeclaredType().Operators.Equality?.Invoke(unexpected, it) ?? false)"), "unexpected == it should have returned false");
   }

   public class with_two_unequal_values_but_operator_inequality_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorInequality);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Must___String.Contain(Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                                                .Must().Throw<AssertionFailedException>()
                                                                .Which.Message), "it.DeclaredType().Operators.InEquality?.Invoke(it, unexpected) ?? true"), "it != unexpected should have returned true");
   }

   public class given_two_unequal_values_but_operator_inequality_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.OperatorInequality);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Must___String.Contain(Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                                                .Must().Throw<AssertionFailedException>()
                                                                .Which.Message), "it.DeclaredType().Operators.InEquality?.Invoke(unexpected, it) ?? true"), "unexpected != it should have returned true");
   }

   // Note: We do NOT test IComparable/IComparable<T>.CompareTo() because: returning 0 doesn't necessarily mean equality.
   // A type can implement CompareTo based on one property (e.g., LastName for sorting) while Equals compares multiple properties.
   //
   // Note: We do NOT test IStructuralComparable.CompareTo() because: returning 0 doesn't necessarily mean structural equality.
   //
   // Note: We do NOT test comparison operators (<, >, <=, >=) because: these define ordering relationships, not equality.
   // For types with partial orderings or custom comparison semantics, comparison results don't reliably indicate equality.

   // Note: No test for GetHashCode - equal hash codes for unequal objects is allowed by the contract

   public class given_two_unequal_values_but_IStructuralEquatable_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IStructuralEquatable);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Must___String.Contain(__Must.Must(Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
                                          .Must().Throw<AssertionFailedException>()
                                          .Which.Message), "it.Equals(unexpected, StructuralEqualityComparer) should have returned false");
   }

   public class given_two_unequal_values_but_IStructuralEquatable_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.IStructuralEquatable);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => Must_Be_NotBe.NotBe(__Must.Must(_actual), _unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("unexpected.Equals(it, StructuralEqualityComparer) should have returned false");
   }
}
