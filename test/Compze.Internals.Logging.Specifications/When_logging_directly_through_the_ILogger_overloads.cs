// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

///<summary>The plain-message and template-plus-values <see cref="ILogger"/> overloads, called through the interface — the forms<br/>
/// callers use when the message is not an interpolated string.</summary>
public class When_logging_directly_through_the_ILogger_overloads
{
   readonly ILogger _logger = new CapturingLogger();
   CapturingLogger Captured => (CapturingLogger)_logger;

   public class via_Critical_with_a_plain_message : When_logging_directly_through_the_ILogger_overloads
   {
      public via_Critical_with_a_plain_message() => _logger.Critical("critical happened");

      [XF] public void the_call_is_recorded_at_Critical_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Critical);
      [XF] public void the_message_is_the_template() => Captured.Captured[0].Template.Must().NotBeNull().Be("critical happened");
   }

   public class via_Critical_with_a_template_and_values : When_logging_directly_through_the_ILogger_overloads
   {
      public via_Critical_with_a_template_and_values() => _logger.Critical("critical {0}", [1]);

      [XF] public void the_call_is_recorded_at_Critical_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Critical);
      [XF] public void the_values_are_captured() => Captured.Captured[0].Values!.Must().SequenceEqual(new object?[] { 1 });
   }

   public class via_Error_with_an_exception_and_a_plain_message : When_logging_directly_through_the_ILogger_overloads
   {
      readonly Exception _reported = new InvalidOperationException("boom");
      public via_Error_with_an_exception_and_a_plain_message() => _logger.Error(_reported, "error happened");

      [XF] public void the_call_is_recorded_at_Error_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Error);
      [XF] public void the_exception_is_captured() => Captured.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_reported);
      [XF] public void the_message_is_the_template() => Captured.Captured[0].Template.Must().NotBeNull().Be("error happened");
   }

   public class via_Warning_with_a_template_and_values : When_logging_directly_through_the_ILogger_overloads
   {
      public via_Warning_with_a_template_and_values() => _logger.Warning("warning {0}", [2]);

      [XF] public void the_call_is_recorded_at_Warning_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Warning);
      [XF] public void the_values_are_captured() => Captured.Captured[0].Values!.Must().SequenceEqual(new object?[] { 2 });
   }

   public class via_Critical_with_an_interpolated_message : When_logging_directly_through_the_ILogger_overloads
   {
      public via_Critical_with_an_interpolated_message() { var x = 3; _logger.Critical($"critical {x}"); }

      [XF] public void the_call_is_recorded_at_Critical_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Critical);
      [XF] public void the_template_is_structured() => Captured.Captured[0].Template.Must().NotBeNull().Be("critical {x}");
   }

   public class via_Critical_with_an_exception_and_a_plain_message : When_logging_directly_through_the_ILogger_overloads
   {
      readonly Exception _reported = new InvalidOperationException("boom");
      public via_Critical_with_an_exception_and_a_plain_message() => _logger.Critical(_reported, "critical happened");

      [XF] public void the_call_is_recorded_at_Critical_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Critical);
      [XF] public void the_exception_is_captured() => Captured.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_reported);
   }

   public class via_Critical_with_an_exception_and_a_template_and_values : When_logging_directly_through_the_ILogger_overloads
   {
      readonly Exception _reported = new InvalidOperationException("boom");
      public via_Critical_with_an_exception_and_a_template_and_values() => _logger.Critical(_reported, "critical {0}", [4]);

      [XF] public void the_values_are_captured() => Captured.Captured[0].Values!.Must().SequenceEqual(new object?[] { 4 });
      [XF] public void the_exception_is_captured() => Captured.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_reported);
   }

   public class via_Error_with_an_exception_and_a_template_and_values : When_logging_directly_through_the_ILogger_overloads
   {
      readonly Exception _reported = new InvalidOperationException("boom");
      public via_Error_with_an_exception_and_a_template_and_values() => _logger.Error(_reported, "error {0}", [5]);

      [XF] public void the_call_is_recorded_at_Error_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Error);
      [XF] public void the_values_are_captured() => Captured.Captured[0].Values!.Must().SequenceEqual(new object?[] { 5 });
   }

   public class via_Warning_with_an_exception_and_a_template_and_values : When_logging_directly_through_the_ILogger_overloads
   {
      readonly Exception _reported = new InvalidOperationException("boom");
      public via_Warning_with_an_exception_and_a_template_and_values() => _logger.Warning(_reported, "warning {0}", [6]);

      [XF] public void the_call_is_recorded_at_Warning_level() => Captured.Captured[0].Level.Must().Be(LogLevel.Warning);
      [XF] public void the_exception_is_captured() => Captured.Captured[0].Exception.Must().NotBeNull().ReferenceEqual(_reported);
   }
}
