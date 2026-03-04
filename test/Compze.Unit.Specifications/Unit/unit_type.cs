using Compze.Must;
using Compze.xUnitBDD;

#pragma warning disable CS8981

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.UnitSpecifications.Unit;

public class unit_type
{
   [XF] public void Value_is_the_default_unit_instance() => unit.Value.Must().Be(default(unit));

   [XF] public void ToString_returns_parentheses() => unit.Value.ToString().Must().Be("()");

   public class equality
   {
      [XF] public void Equals_returns_true_for_another_unit() => unit.Value.Equals(unit.Value).Must().BeTrue();

      [XF] public void Equals_returns_true_for_a_boxed_unit() => unit.Value.Equals((object)unit.Value).Must().BeTrue();

      // ReSharper disable once SuspiciousTypeConversion.Global
      [XF] public void Equals_returns_false_for_a_non_unit_object() => unit.Value.Equals("not a unit").Must().BeFalse();

      [XF] public void Equals_returns_false_for_null() => unit.Value.Equals(null).Must().BeFalse();

      [XF] public void the_equality_operator_returns_true_for_two_unit_values() => (unit.Value == default(unit)).Must().BeTrue();

      [XF] public void the_inequality_operator_returns_false_for_two_unit_values() => (unit.Value != default(unit)).Must().BeFalse();

      [XF] public void GetHashCode_returns_the_same_value_for_all_instances() => unit.Value.GetHashCode().Must().Be(unit.Value.GetHashCode());
   }

   public class comparison
   {
      [XF] public void CompareTo_returns_zero_for_another_unit() => unit.Value.CompareTo(unit.Value).Must().Be(0);
   }

   public class implicit_conversion
   {
      [XF] public void Unit_converts_implicitly_to_ValueTuple()
      {
         ValueTuple vt = unit.Value;
         vt.Must().Be(default(ValueTuple));
      }

      [XF] public void ValueTuple_converts_implicitly_to_Unit()
      {
         unit u = default(ValueTuple);
         u.Must().Be(unit.Value);
      }
   }

   public class From_method
   {
      [XF] public void executes_the_action_and_returns_unit()
      {
         var executed = false;
         var result = unit.Invoke(() => executed = true);
         executed.Must().BeTrue();
         result.Must().Be(unit.Value);
      }
   }

   public class InvokeAsync_method
   {
      [XF] public async Task executes_the_async_action_and_returns_unit()
      {
         var executed = false;
         var result = await unit.InvokeAsync(async () => { await Task.Yield(); executed = true; });
         executed.Must().BeTrue();
         result.Must().Be(unit.Value);
      }
   }


}
