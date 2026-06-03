using Compze.Must;
using Compze.xUnitBDD;

#pragma warning disable CS8981

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

// ReSharper disable once CheckNamespace Folder is 'Unit', but a '.Unit' namespace segment would collide with the Unit type under test (Unit.Value).
namespace Compze.UnitSpecifications;

public class unit_type
{
   [XF] public void Value_is_the_default_unit_instance() => Unit.Value.Must().Be(default(Unit));

   [XF] public void ToString_returns_parentheses() => Unit.Value.ToString().Must().Be("()");

   public class equality
   {
      [XF] public void Equals_returns_true_for_another_unit() => Unit.Value.Equals(Unit.Value).Must().BeTrue();

      [XF] public void Equals_returns_true_for_a_boxed_unit() => Unit.Value.Equals((object)Unit.Value).Must().BeTrue();

      // ReSharper disable once SuspiciousTypeConversion.Global
      [XF] public void Equals_returns_false_for_a_non_unit_object() => Unit.Value.Equals("not a unit").Must().BeFalse();

      [XF] public void Equals_returns_false_for_null() => Unit.Value.Equals(null).Must().BeFalse();

      [XF] public void the_equality_operator_returns_true_for_two_unit_values() => (Unit.Value == default(Unit)).Must().BeTrue();

      [XF] public void the_inequality_operator_returns_false_for_two_unit_values() => (Unit.Value != default(Unit)).Must().BeFalse();

      [XF] public void GetHashCode_returns_the_same_value_for_all_instances() => Unit.Value.GetHashCode().Must().Be(Unit.Value.GetHashCode());
   }

   public class comparison
   {
      [XF] public void CompareTo_returns_zero_for_another_unit() => Unit.Value.CompareTo(Unit.Value).Must().Be(0);
   }

   public class implicit_conversion
   {
      [XF] public void Unit_converts_implicitly_to_ValueTuple()
      {
         ValueTuple vt = Unit.Value;
         vt.Must().Be(default(ValueTuple));
      }

      [XF] public void ValueTuple_converts_implicitly_to_Unit()
      {
         Unit u = default(ValueTuple);
         u.Must().Be(Unit.Value);
      }
   }

   public class From_method
   {
      [XF] public void executes_the_action_and_returns_unit()
      {
         var executed = false;
         var result = Unit.Invoke(() => executed = true);
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }

   public class InvokeAsync_method
   {
      [XF] public async Task executes_the_async_action_and_returns_unit()
      {
         var executed = false;
         var result = await Unit.InvokeAsync(async () => { await Task.Yield(); executed = true; });
         executed.Must().BeTrue();
         result.Must().Be(Unit.Value);
      }
   }


}
