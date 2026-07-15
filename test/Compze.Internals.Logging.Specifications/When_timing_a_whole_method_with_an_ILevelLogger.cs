// ReSharper disable InconsistentNaming



namespace Compze.Internals.Logging.Specifications;

public class When_timing_a_whole_method_with_an_ILevelLogger
{
   readonly CapturingLogger _logger = new();

   public class with_the_delegate_form : When_timing_a_whole_method_with_an_ILevelLogger
   {
      readonly int _result;
      public with_the_delegate_form() => _result = Decode();
      int Decode() => _logger.Debug().MethodExecutionTime(() => 42);

      [XF] public void the_methods_result_is_returned() => _result.Must().Be(42);
      [XF] public void two_lines_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_label_is_the_method_name_not_the_delegate_source_text() => _logger.Captured[0].Values![1].Must().Be("Decode");
      [XF] public void the_completion_line_reports_the_elapsed_time() => _logger.Captured[1].Template.Must().NotBeNull().Be("{spanPath} {label} took {elapsedMs:F1}ms");
   }

   public class with_the_using_scope_form : When_timing_a_whole_method_with_an_ILevelLogger
   {
      public with_the_using_scope_form() => Render();
      void Render() { using var _ = _logger.Debug().MethodExecutionTime(); }

      [XF] public void two_lines_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_label_is_the_method_name() => _logger.Captured[0].Values![1].Must().Be("Render");
   }

   public class with_the_asynchronous_form : When_timing_a_whole_method_with_an_ILevelLogger
   {
      readonly int _result;
      public with_the_asynchronous_form() => _result = Load().GetAwaiter().GetResult();
      Task<int> Load() => _logger.Debug().MethodExecutionTimeAsync(async () => { await Task.Yield(); return 7; });

      [XF] public void the_methods_result_is_returned() => _result.Must().Be(7);
      [XF] public void the_label_is_the_method_name() => _logger.Captured[0].Values![1].Must().Be("Load");
   }

   public class that_throws : When_timing_a_whole_method_with_an_ILevelLogger
   {
      readonly Exception _thrown;
      public that_throws() => _thrown = Invoking(Decode).Must().Throw<InvalidOperationException>().Which;
      int Decode() => _logger.Debug().MethodExecutionTime(ThrowingWork);
      static int ThrowingWork() => throw new InvalidOperationException("boom");

      [XF] public void the_exception_propagates() => _thrown.Message.Must().Be("boom");
      [XF] public void the_fault_line_is_labelled_with_the_method_name() => _logger.Captured[1].Values![1].Must().Be("Decode");
      [XF] public void the_fault_type_is_captured() => _logger.Captured[1].Values![3].Must().Be("InvalidOperationException");
   }

   public class when_the_level_is_disabled : When_timing_a_whole_method_with_an_ILevelLogger
   {
      readonly CapturingLogger _recorder = new();
      int _bodyRunCount;
      readonly int _result;
      public when_the_level_is_disabled() => _result = Decode();
      int Decode() => _recorder.WithLogLevel(LogLevel.Warning).Debug().MethodExecutionTime(() => { _bodyRunCount++; return 5; });

      [XF] public void no_lines_are_logged() => _recorder.Captured.Must().BeEmpty();
      [XF] public void the_method_body_still_runs_exactly_once() => _bodyRunCount.Must().Be(1);
      [XF] public void the_methods_result_is_returned() => _result.Must().Be(5);
   }
}
