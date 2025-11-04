using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;
using AssertionFailedException = Compze.Utilities.Testing.Must.AssertionFailedException;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CA1711 // ending name on Exception

namespace Compze.Utilities.Tests.Testing.Must;

public class When_calling_Must_NotBe_with_custom_types : UniversalTestBase
{
   public class given_two_unequal_values_with_all_methods_working : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_does_not_throw() => _actual.Must().NotBe(_unexpected);
   }

   public class with_two_equal_values_with_all_methods_working : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(42);

      [XF] public void it_throws() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>();
   }

   public class with_two_unequal_values_but_Object_Equals_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.ObjectEquals);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_the_full_message_is() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Be("""
                                    
                                    --------------------------------------------------
                                    Failing assertion:
                                    --------------------------------------------------
                                    _actual.Must().NotBe(_unexpected)
                                    --------------------------------------------------
                                    first failing test: 
                                       it => !Equals(it, unexpected)
                                    --------------------------------------------------
                                    Diff:
                                    --------------------------------------------------
                                    --- expected
                                    +++ actual
                                    @@ -1,5 +1,5 @@
                                     {
                                       "$type": "Compze.Utilities.Tests.Testing.Must.ComparableWithErrorInjectionSupport, Compze.Utilities.Tests",
                                    -  "_breakComparableMethod": 0,
                                    -  "_value": 99
                                    +  "_breakComparableMethod": 1,
                                    +  "_value": 42
                                     }
                                    
                                    --------------------------------------------------
                                    _actual was:
                                    --------------------------------------------------
                                    ToString():
                                    --------------------------------------------------
                                    Compze.Utilities.Tests.Testing.Must.ComparableWithErrorInjectionSupport
                                    --------------------------------------------------
                                    JSON:
                                    --------------------------------------------------
                                    {
                                      "$type": "Compze.Utilities.Tests.Testing.Must.ComparableWithErrorInjectionSupport, Compze.Utilities.Tests",
                                      "_breakComparableMethod": 1,
                                      "_value": 42
                                    }
                                    --------------------------------------------------
                                    _unexpected was:
                                    --------------------------------------------------
                                    ToString():
                                    --------------------------------------------------
                                    Compze.Utilities.Tests.Testing.Must.ComparableWithErrorInjectionSupport
                                    --------------------------------------------------
                                    JSON:
                                    --------------------------------------------------
                                    {
                                      "$type": "Compze.Utilities.Tests.Testing.Must.ComparableWithErrorInjectionSupport, Compze.Utilities.Tests",
                                      "_breakComparableMethod": 0,
                                      "_value": 99
                                    }
                                    --------------------------------------------------
                                    """);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => !Equals(it, unexpected)");
   }

   public class with_two_unequal_values_but_Object_Equals_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.ObjectEquals);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => !Equals(unexpected, it)");
   }

   public class with_two_unequal_values_but_IEquatable_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IEquatable);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => !((it as IEquatable<TValue>)?.Equals(unexpected) ?? false)");
   }

   public class with_two_unequal_values_but_IEquatable_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.IEquatable);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it => !((unexpected as IEquatable<TValue>)?.Equals(it) ?? false)");
   }

   public class with_two_unequal_values_but_operator_equality_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorEquality);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which
           .Message.Must().Contain("!(it.DeclaredType().Operators.Equality?.Invoke(it, unexpected) ?? false)").Contain("it == unexpected should have returned false");
   }

   public class given_two_unequal_values_but_operator_equality_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.OperatorEquality);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which
           .Message.Must().Contain("!(it.DeclaredType().Operators.Equality?.Invoke(unexpected, it) ?? false)").Contain("unexpected == it should have returned false");
   }

   public class with_two_unequal_values_but_operator_inequality_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.OperatorInequality);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it.DeclaredType().Operators.InEquality?.Invoke(it, unexpected) ?? true").Contain("it != unexpected should have returned true");
   }

   public class given_two_unequal_values_but_operator_inequality_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.OperatorInequality);

      [XF] public void it_throws_and_includes_the_failing_predicate_and_custom_message_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it.DeclaredType().Operators.InEquality?.Invoke(unexpected, it) ?? true").Contain("unexpected != it should have returned true");
   }

   public class given_two_unequal_values_but_IStructuralEquatable_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42, BreakComparableMethod.IStructuralEquatable);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("it.Equals(unexpected, StructuralEqualityComparer) should have returned false");
   }

   public class given_two_unequal_values_but_IStructuralEquatable_reversed_is_broken : When_calling_Must_NotBe_with_custom_types
   {
      readonly ComparableWithErrorInjectionSupport _actual = new(42);
      readonly ComparableWithErrorInjectionSupport _unexpected = new(99, BreakComparableMethod.IStructuralEquatable);

      [XF] public void it_throws_and_includes_the_failing_predicate_in_the_message() =>
         Invoking(() => _actual.Must().NotBe(_unexpected))
           .Must().Throw<AssertionFailedException>()
           .Which.Message.Must().Contain("unexpected.Equals(it, StructuralEqualityComparer) should have returned false");
   }
}
