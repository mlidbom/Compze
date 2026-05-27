// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

public class When_logging_at_a_level_that_is_not_enabled
{
   public class via_an_interpolated_string_handler : When_logging_at_a_level_that_is_not_enabled
   {
      readonly CapturingLogger _logger;
      int _expensiveExpressionEvaluationCount;
      int ExpensiveExpression() { _expensiveExpressionEvaluationCount++; return 42; }

      public via_an_interpolated_string_handler()
      {
         _logger = (CapturingLogger)new CapturingLogger().WithLogLevel(LogLevel.Warning);
         _logger.Debug($"value is {ExpensiveExpression()}");
         _logger.Info($"value is {ExpensiveExpression()}");
      }

      [XF] public void no_log_call_is_recorded() => _logger.Captured.Must().BeEmpty();
      [XF] public void expressions_inside_the_holes_are_never_evaluated() => _expensiveExpressionEvaluationCount.Must().Be(0);
   }

   public class via_a_plain_string_overload : When_logging_at_a_level_that_is_not_enabled
   {
      readonly CapturingLogger _logger;
      public via_a_plain_string_overload()
      {
         _logger = (CapturingLogger)new CapturingLogger().WithLogLevel(LogLevel.Warning);
         _logger.Debug("disabled debug");
         _logger.Info("disabled info");
      }

      [XF] public void no_log_call_is_recorded() => _logger.Captured.Must().BeEmpty();
   }

   public class via_an_interpolated_string_handler_while_logging_is_globally_suppressed : When_logging_at_a_level_that_is_not_enabled
   {
      int _expensiveExpressionEvaluationCount;
      int ExpensiveExpression() { _expensiveExpressionEvaluationCount++; return 42; }

      readonly CapturingLogger _logger = new();

      public via_an_interpolated_string_handler_while_logging_is_globally_suppressed()
         => CompzeLogger.SuppressLoggingWhileRunningAsync(() =>
         {
            _logger.Info($"value is {ExpensiveExpression()}");
            return Task.CompletedTask;
         }).GetAwaiter().GetResult();

      [XF] public void no_log_call_is_recorded() => _logger.Captured.Must().BeEmpty();
      [XF] public void expressions_inside_the_holes_are_never_evaluated() => _expensiveExpressionEvaluationCount.Must().Be(0);
   }
}
