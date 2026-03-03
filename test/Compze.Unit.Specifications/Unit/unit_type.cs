using System.Threading.Tasks;
using Compze.Underscore;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

#pragma warning disable CA1052 // BDD-style nested specification classes cannot be static

namespace Compze.Unit.Specifications.Unit;

public class unit_type
{
   [XF] public void Value_is_the_default_unit_instance() => unit.Value.Must().Be(default(unit));

   [XF] public void ToString_returns_parentheses() => unit.Value.ToString().Must().Be("()");

#pragma warning disable CS8981
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
#pragma warning restore CS8981

   public class From_method
   {
      [XF] public void executes_the_action_and_returns_unit()
      {
         var executed = false;
         var result = unit.From(() => executed = true);
         executed.Must().BeTrue();
         result.Must().Be(unit.Value);
      }
   }

   public static class Func_method
   {
      public class with_zero_parameters
      {
         [XF] public void wraps_the_action_into_a_Func_returning_unit()
         {
            var executed = false;
            var func = unit.Func(() => { executed = true; });
            var result = func();
            executed.Must().BeTrue();
            result.Must().Be(unit.Value);
         }
      }

      public class with_one_parameter
      {
         [XF] public void passes_the_parameter_to_the_wrapped_action()
         {
            var captured = "";
            var func = unit.Func<string>(s => { captured = s; });
            var result = func("hello");
            captured.Must().Be("hello");
            result.Must().Be(unit.Value);
         }
      }

      public class with_two_parameters
      {
         [XF] public void passes_both_parameters_to_the_wrapped_action()
         {
            var capturedA = "";
            var capturedB = 0;
            var func = unit.Func<string, int>((s, i) => { capturedA = s; capturedB = i; });
            var result = func("hello", 42);
            capturedA.Must().Be("hello");
            capturedB.Must().Be(42);
            result.Must().Be(unit.Value);
         }
      }
   }

   public static class AsyncFunc_method
   {
      public class with_zero_parameters
      {
         [XF] public async Task wraps_the_async_action_into_a_Func_returning_Task_of_unit()
         {
            var executed = false;
            var func = unit.AsyncFunc(async () => { await Task.Yield(); executed = true; });
            var result = await func();
            executed.Must().BeTrue();
            result.Must().Be(unit.Value);
         }
      }

      public class with_one_parameter
      {
         [XF] public async Task passes_the_parameter_to_the_wrapped_async_action()
         {
            var captured = "";
            var func = unit.AsyncFunc<string>(async s => { await Task.Yield(); captured = s; });
            var result = await func("hello");
            captured.Must().Be("hello");
            result.Must().Be(unit.Value);
         }
      }

      public class with_two_parameters
      {
         [XF] public async Task passes_both_parameters_to_the_wrapped_async_action()
         {
            var capturedA = "";
            var capturedB = 0;
            var func = unit.AsyncFunc<string, int>(async (s, i) => { await Task.Yield(); capturedA = s; capturedB = i; });
            var result = await func("hello", 42);
            capturedA.Must().Be("hello");
            capturedB.Must().Be(42);
            result.Must().Be(unit.Value);
         }
      }
   }
}
