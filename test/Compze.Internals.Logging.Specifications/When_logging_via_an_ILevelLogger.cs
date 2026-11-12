// ReSharper disable InconsistentNaming



namespace Compze.Internals.Logging.Specifications;

public class When_logging_via_an_ILevelLogger
{
   public class via_Trace : When_logging_via_an_ILevelLogger
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Trace);
      public via_Trace() { var x = 0; _logger.Trace().Log($"t {x}"); }

      [XF] public void the_call_is_recorded_at_Trace_level() => _backend.Captured[0].Level.Must().Be(LogLevel.Trace);
      [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("t {x}");
   }

   public class via_Debug : When_logging_via_an_ILevelLogger
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Debug);
      public via_Debug() { var x = 1; _logger.Debug().Log($"d {x}"); }

      [XF] public void the_call_is_recorded_at_Debug_level() => _backend.Captured[0].Level.Must().Be(LogLevel.Debug);
      [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("d {x}");
      [XF] public void the_value_is_captured() => _backend.Captured[0].TemplateValues.Must().SequenceEqual(new object?[] { 1 });
   }

   public class via_Info : When_logging_via_an_ILevelLogger
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Debug);
      public via_Info() { var x = 2; _logger.Info().Log($"i {x}"); }

      [XF] public void the_call_is_recorded_at_Info_level() => _backend.Captured[0].Level.Must().Be(LogLevel.Info);
      [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("i {x}");
   }

   public class via_Warning : When_logging_via_an_ILevelLogger
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Debug);
      public via_Warning() { var x = 3; _logger.Warning().Log($"w {x}"); }

      [XF] public void the_call_is_recorded_at_Warning_level() => _backend.Captured[0].Level.Must().Be(LogLevel.Warning);
      [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("w {x}");
   }

   public class via_Error : When_logging_via_an_ILevelLogger
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Debug);
      public via_Error() { var x = 4; _logger.Error().Log($"e {x}"); }

      [XF] public void the_call_is_recorded_at_Error_level() => _backend.Captured[0].Level.Must().Be(LogLevel.Error);
      [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("e {x}");
   }

   public class via_Critical : When_logging_via_an_ILevelLogger
   {
      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Debug);
      public via_Critical() { var x = 6; _logger.Critical().Log($"c {x}"); }

      [XF] public void the_call_is_recorded_at_Critical_level() => _backend.Captured[0].Level.Must().Be(LogLevel.Critical);
      [XF] public void the_template_is_structured() => _backend.Captured[0].MessageTemplate.Must().Be("c {x}");
   }

   public class when_the_level_is_not_enabled : When_logging_via_an_ILevelLogger
   {
      int _expensiveExpressionEvaluationCount;
      int ExpensiveExpression() { _expensiveExpressionEvaluationCount++; return 99; }

      readonly CapturingLogBackend _backend = CapturingLogBackend.Install();
      readonly ILogger _logger;
      public when_the_level_is_not_enabled()
      {
         _logger = CompzeLogger.For<When_logging_via_an_ILevelLogger>().WithLogLevel(LogLevel.Warning);
         _logger.Debug().Log($"value {ExpensiveExpression()}");
      }

      [XF] public void no_log_call_is_recorded() => _backend.Captured.Must().BeEmpty();
      [XF] public void expressions_inside_the_holes_are_never_evaluated() => _expensiveExpressionEvaluationCount.Must().Be(0);
   }
}
