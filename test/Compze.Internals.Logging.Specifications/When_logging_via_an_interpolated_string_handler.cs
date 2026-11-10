// ReSharper disable InconsistentNaming



namespace Compze.Internals.Logging.Specifications;

public class When_logging_via_an_interpolated_string_handler
{
   public class with_a_single_hole_containing_a_local_variable : When_logging_via_an_interpolated_string_handler
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
      public with_a_single_hole_containing_a_local_variable()
      {
         var userId = 42;
         _logger.Info($"user {userId} signed in");
      }

      [XF] public void the_template_carries_the_property_name_from_the_expression()
         => _backend.Captured[0].MessageTemplate.Must().Be("user {userId} signed in");

      [XF] public void the_value_is_captured_in_order()
         => _backend.Captured[0].TemplateValues.Must().SequenceEqual(new object?[] { 42 });
   }

   public class with_multiple_holes : When_logging_via_an_interpolated_string_handler
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
      public with_multiple_holes()
      {
         var userId = 7;
         var action = "login";
         _logger.Info($"user {userId} performed {action}");
      }

      [XF] public void the_template_names_each_hole_left_to_right()
         => _backend.Captured[0].MessageTemplate.Must().Be("user {userId} performed {action}");

      [XF] public void the_values_are_captured_left_to_right()
         => _backend.Captured[0].TemplateValues.Must().SequenceEqual(new object?[] { 7, "login" });
   }

   public class with_a_format_specifier : When_logging_via_an_interpolated_string_handler
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
      public with_a_format_specifier()
      {
         var elapsed = TimeSpan.FromSeconds(1.5);
         _logger.Info($"backoff {elapsed.TotalSeconds:F1}s");
      }

      [XF] public void the_format_specifier_is_carried_into_the_template()
         => _backend.Captured[0].MessageTemplate.Must().Be("backoff {elapsed_TotalSeconds:F1}s");
   }

   public class with_alignment : When_logging_via_an_interpolated_string_handler
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
      public with_alignment()
      {
         var count = 3;
         _logger.Info($"count={count,5}");
      }

      [XF] public void the_alignment_is_carried_into_the_template()
         => _backend.Captured[0].MessageTemplate.Must().Be("count={count,5}");
   }

   public class with_alignment_and_format_specifier : When_logging_via_an_interpolated_string_handler
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
      public with_alignment_and_format_specifier()
      {
         var ratio = 0.4567;
         _logger.Info($"ratio={ratio,8:F2}");
      }

      [XF] public void both_appear_in_the_template_in_the_correct_order()
         => _backend.Captured[0].MessageTemplate.Must().Be("ratio={ratio,8:F2}");
   }

   public class for_each_log_level : When_logging_via_an_interpolated_string_handler
   {
      public class via_Trace : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Trace);
         public via_Trace() { var x = 0; _logger.Trace($"t {x}"); }
         [XF] public void the_captured_level_is_Trace() => _backend.Captured[0].Level.Must().Be(LogLevel.Trace);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("t {x}");
      }

      public class via_Debug : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         public via_Debug() { var x = 1; _logger.Debug($"d {x}"); }
         [XF] public void the_captured_level_is_Debug() => _backend.Captured[0].Level.Must().Be(LogLevel.Debug);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("d {x}");
      }

      public class via_Info : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         public via_Info() { var x = 2; _logger.Info($"i {x}"); }
         [XF] public void the_captured_level_is_Info() => _backend.Captured[0].Level.Must().Be(LogLevel.Info);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("i {x}");
      }

      public class via_Warning : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         public via_Warning() { var x = 3; _logger.Warning($"w {x}"); }
         [XF] public void the_captured_level_is_Warning() => _backend.Captured[0].Level.Must().Be(LogLevel.Warning);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("w {x}");
      }

      public class via_Warning_with_exception : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         readonly Exception _exception = new InvalidOperationException("boom");
         public via_Warning_with_exception() { var x = 4; _logger.Warning(_exception, $"w {x}"); }
         [XF] public void the_exception_is_carried_through() => _backend.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_exception);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("w {x}");
      }

      public class via_Error_with_exception : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         readonly Exception _exception = new InvalidOperationException("boom");
         public via_Error_with_exception() { var x = 5; _logger.Error(_exception, $"e {x}"); }
         [XF] public void the_captured_level_is_Error() => _backend.Captured[0].Level.Must().Be(LogLevel.Error);
         [XF] public void the_exception_is_carried_through() => _backend.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_exception);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("e {x}");
      }

      public class via_Critical : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         public via_Critical() { var x = 6; _logger.Critical($"c {x}"); }
         [XF] public void the_captured_level_is_Critical() => _backend.Captured[0].Level.Must().Be(LogLevel.Critical);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("c {x}");
      }

      public class via_Critical_with_exception : for_each_log_level
      {
         readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
         readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_interpolated_string_handler>().WithLogLevel(LogLevel.Debug);
         readonly Exception _exception = new InvalidOperationException("boom");
         public via_Critical_with_exception() { var x = 7; _logger.Critical(_exception, $"c {x}"); }
         [XF] public void the_captured_level_is_Critical() => _backend.Captured[0].Level.Must().Be(LogLevel.Critical);
         [XF] public void the_exception_is_carried_through() => _backend.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_exception);
         [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("c {x}");
      }
   }
}
