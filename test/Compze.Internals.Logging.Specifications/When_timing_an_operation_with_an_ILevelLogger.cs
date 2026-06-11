// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

public class When_timing_an_operation_with_an_ILevelLogger
{
   readonly CapturingLogger _logger = new();

   public class that_returns_a_value : When_timing_an_operation_with_an_ILevelLogger
   {
      readonly int _result;
      public that_returns_a_value() => _result = _logger.Debug().ExecutionTime(() => 42);

      [XF] public void the_operations_result_is_returned() => _result.Must().Be(42);
      [XF] public void two_lines_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_first_line_announces_the_start() => _logger.Captured[0].Template.Must().NotBeNull().Be("{spanPath} {label} started");
      [XF] public void the_second_line_reports_the_elapsed_time() => _logger.Captured[1].Template.Must().NotBeNull().Be("{spanPath} {label} took {elapsedMs:F1}ms");
      [XF] public void the_label_is_the_source_text_of_the_operation() => _logger.Captured[0].Values![1].Must().Be("() => 42");
      [XF] public void both_lines_share_the_same_span_path() => _logger.Captured[1].Values![0].Must().Be(_logger.Captured[0].Values![0]);
      [XF] public void the_elapsed_time_is_captured_as_a_structured_double() => (_logger.Captured[1].Values![2] is double).Must().Be(true);
   }

   public class that_is_described_by_an_explicit_label : When_timing_an_operation_with_an_ILevelLogger
   {
      public that_is_described_by_an_explicit_label() => _logger.Debug().ExecutionTime("decode wallpaper", () => 42);

      [XF] public void the_explicit_label_is_used_not_the_source_text() => _logger.Captured[0].Values![1].Must().Be("decode wallpaper");
   }

   public class that_is_timed_with_a_using_scope : When_timing_an_operation_with_an_ILevelLogger
   {
      public that_is_timed_with_a_using_scope() { using(_logger.Debug().ExecutionTime("rendering")) {} }

      [XF] public void two_lines_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_given_label_is_used() => _logger.Captured[0].Values![1].Must().Be("rendering");
      [XF] public void the_second_line_reports_the_elapsed_time() => _logger.Captured[1].Template.Must().NotBeNull().Be("{spanPath} {label} took {elapsedMs:F1}ms");
   }

   public class that_throws : When_timing_an_operation_with_an_ILevelLogger
   {
      readonly Exception _thrown;
      public that_throws() => _thrown = Invoking(() => _logger.Debug().ExecutionTime(ThrowingOperation)).Must().Throw<InvalidOperationException>().Which;
      static int ThrowingOperation() => throw new InvalidOperationException("boom");

      [XF] public void the_exception_propagates() => _thrown.Message.Must().Be("boom");
      [XF] public void two_lines_are_still_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_second_line_reports_the_fault() => _logger.Captured[1].Template.Must().NotBeNull().Be("{spanPath} {label} FAULTED after {elapsedMs:F1}ms: {faultType}");
      [XF] public void the_fault_type_is_captured() => _logger.Captured[1].Values![3].Must().Be("InvalidOperationException");
   }

   public class that_is_asynchronous : When_timing_an_operation_with_an_ILevelLogger
   {
      readonly int _result;
      public that_is_asynchronous() => _result = AwaitTimedOperation().GetAwaiter().GetResult();
      async Task<int> AwaitTimedOperation() => await _logger.Debug().ExecutionTimeAsync(async () => { await Task.Yield(); return 7; });

      [XF] public void the_operations_result_is_returned() => _result.Must().Be(7);
      [XF] public void two_lines_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_second_line_reports_the_elapsed_time() => _logger.Captured[1].Template.Must().NotBeNull().Be("{spanPath} {label} took {elapsedMs:F1}ms");
   }

   public class that_contains_a_nested_timed_operation : When_timing_an_operation_with_an_ILevelLogger
   {
      public that_contains_a_nested_timed_operation() => _logger.Debug().ExecutionTime("outer", () => _logger.Debug().ExecutionTime("inner", () => 0));

      [XF] public void all_four_lines_are_logged() => _logger.Captured.Count.Must().Be(4);
      [XF] public void the_outer_span_path_has_no_parent() => ((string)_logger.Captured[0].Values![0]!).Contains('/', StringComparison.Ordinal).Must().Be(false);
      [XF] public void the_inner_span_path_is_rooted_in_the_outer_span() => ((string)_logger.Captured[1].Values![0]!).StartsWith((string)_logger.Captured[0].Values![0]! + "/", StringComparison.Ordinal).Must().Be(true);
   }

   public class when_the_level_is_disabled : When_timing_an_operation_with_an_ILevelLogger
   {
      readonly CapturingLogger _recorder = new();
      int _operationRunCount;
      readonly int _result;
      public when_the_level_is_disabled() => _result = _recorder.WithLogLevel(LogLevel.Warning).Debug().ExecutionTime(() => { _operationRunCount++; return 5; });

      [XF] public void no_lines_are_logged() => _recorder.Captured.Must().BeEmpty();
      [XF] public void the_operation_still_runs_exactly_once() => _operationRunCount.Must().Be(1);
      [XF] public void the_operations_result_is_returned() => _result.Must().Be(5);
   }
}
