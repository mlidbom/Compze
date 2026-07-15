// ReSharper disable InconsistentNaming



namespace Compze.Internals.Logging.Specifications;

public class When_a_property_name_is_captured_from_an_expression
{
   public class that_is_a_simple_local_variable : When_a_property_name_is_captured_from_an_expression
   {
      readonly CapturingLogger _logger = new();
      public that_is_a_simple_local_variable() { var simpleName = 1; _logger.Info($"{simpleName}"); }

      [XF] public void the_property_name_in_the_template_is_the_variable_name()
         => _logger.Captured[0].Template.Must().NotBeNull().Be("{simpleName}");
   }

   public class that_contains_a_member_access : When_a_property_name_is_captured_from_an_expression
   {
      readonly CapturingLogger _logger = new();
      public that_contains_a_member_access() { var ts = TimeSpan.FromSeconds(1); _logger.Info($"{ts.TotalSeconds}"); }

      [XF] public void dots_are_replaced_with_underscores_to_make_a_valid_property_name()
         => _logger.Captured[0].Template.Must().NotBeNull().Be("{ts_TotalSeconds}");
   }

   public class that_contains_a_method_call : When_a_property_name_is_captured_from_an_expression
   {
      readonly CapturingLogger _logger = new();
      public that_contains_a_method_call() => _logger.Info($"{GetValue()}");
      static int GetValue() => 7;

      [XF] public void parentheses_are_replaced_with_underscores_to_make_a_valid_property_name()
         => _logger.Captured[0].Template.Must().NotBeNull().Be("{GetValue__}");
   }

}
