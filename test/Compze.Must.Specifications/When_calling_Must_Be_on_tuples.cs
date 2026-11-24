// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_calling_Must_Be_on_tuples : UniversalTestBase
{
   public class whose_items_include_a_value_that_cannot_be_compared : When_calling_Must_Be_on_tuples
   {
      public class that_are_equal_and_hold_distinct_instances : whose_items_include_a_value_that_cannot_be_compared
      {
         [XF] public void Be_succeeds_on_value_tuples() =>
            (new ValueThatCannotBeCompared("same"), 1).Must().Be((new ValueThatCannotBeCompared("same"), 1));

         [XF] public void Be_succeeds_on_Tuples() =>
            Tuple.Create(new ValueThatCannotBeCompared("same"), 1).Must().Be(Tuple.Create(new ValueThatCannotBeCompared("same"), 1));

         [XF] public void Be_succeeds_on_value_tuples_holding_it_in_a_nested_value_tuple() =>
            (1, (new ValueThatCannotBeCompared("same"), 2)).Must().Be((1, (new ValueThatCannotBeCompared("same"), 2)));
      }

      public class that_are_unequal : whose_items_include_a_value_that_cannot_be_compared
      {
         [XF] public void Be_throws_an_AssertionFailedException() =>
            Invoking(() => (new ValueThatCannotBeCompared("actual"), 1).Must().Be((new ValueThatCannotBeCompared("expected"), 1)))
              .Must().Throw<AssertionFailedException>();
      }
   }

   public class whose_items_can_all_be_compared : When_calling_Must_Be_on_tuples
   {
      public class that_are_equal_but_an_items_IComparable_T_does_not_return_0 : whose_items_can_all_be_compared
      {
         [XF] public void Be_throws_naming_the_tuples_IComparable_T() =>
            Invoking(() => (new ComparableWithErrorInjectionSupport(42, BreakComparableMethod.IComparableGeneric), 1).Must().Be((new ComparableWithErrorInjectionSupport(42), 1)))
              .Must().Throw<AssertionFailedException>()
              .Which.Message.Must().Contain("it.CompareTo(expected) (IComparable<T>) should have returned 0");
      }
   }

   class ValueThatCannotBeCompared(string value) : IEquatable<ValueThatCannotBeCompared>
   {
      readonly string _value = value;

      public bool Equals(ValueThatCannotBeCompared? other) => other is not null && _value == other._value;
      public override bool Equals(object? obj) => obj is ValueThatCannotBeCompared other && Equals(other);
      public override int GetHashCode() => _value.GetHashCode(StringComparison.Ordinal);
   }
}
