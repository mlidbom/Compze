// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

public class When_logging_within_an_activity
{
   readonly CapturingLogger _logger = new();

   public class that_has_just_been_started : When_logging_within_an_activity
   {
      public that_has_just_been_started() => _logger.StartActivity("overlay show");

      [XF] public void a_started_line_is_logged() => _logger.Captured[0].Template.Must().NotBeNull().Be("activity started");
      [XF] public void the_started_line_is_tagged_with_the_activity_name() => _logger.Captured[0].ActivityName.Must().NotBeNull().Be("overlay show");
      [XF] public void the_started_line_is_tagged_with_an_activity_id() => _logger.Captured[0].ActivityId.Must().NotBeNull();
   }

   public class within_which_a_separate_logger_logs : When_logging_within_an_activity
   {
      public within_which_a_separate_logger_logs() { using(_logger.StartActivity("overlay show")) { _logger.Debug().Log("preparing surfaces"); } }

      [XF] public void the_separate_loggers_line_is_tagged_with_the_activity_even_though_it_did_not_go_through_the_activity_handle()
         => _logger.Captured[1].ActivityName.Must().NotBeNull().Be("overlay show");
   }

   public class within_which_an_operation_is_timed : When_logging_within_an_activity
   {
      public within_which_an_operation_is_timed() { using(_logger.StartActivity("overlay show")) { _logger.Debug().ExecutionTime(() => 42); } }

      [XF] public void the_span_start_line_is_tagged_with_the_activity() => _logger.Captured[1].ActivityName.Must().NotBeNull().Be("overlay show");
      [XF] public void the_span_completion_line_is_tagged_with_the_activity() => _logger.Captured[2].ActivityName.Must().NotBeNull().Be("overlay show");
   }

   public class within_which_a_higher_level_is_logged : When_logging_within_an_activity
   {
      public within_which_a_higher_level_is_logged() { using(_logger.StartActivity("overlay show")) { _logger.Warning("disk almost full"); } }

      [XF] public void the_warning_is_recorded_at_warning_level() => _logger.Captured[1].Level.Must().Be(LogLevel.Warning);
      [XF] public void the_warning_is_tagged_with_the_activity_even_though_the_activity_runs_at_a_lower_level() => _logger.Captured[1].ActivityName.Must().NotBeNull().Be("overlay show");
   }

   public class within_which_work_is_awaited : When_logging_within_an_activity
   {
      public within_which_work_is_awaited() => LogAfterAnAwait().GetAwaiter().GetResult();
      async Task LogAfterAnAwait()
      {
         using(_logger.StartActivity("overlay show"))
         {
            await Task.Yield();
            _logger.Debug().Log("resumed after the await");
         }
      }

      [XF] public void logging_after_an_await_is_still_tagged_with_the_activity_because_it_flows_with_the_async_context()
         => _logger.Captured[1].ActivityName.Must().NotBeNull().Be("overlay show");
   }

   public class that_logs_an_elapsed_milestone : When_logging_within_an_activity
   {
      public that_logs_an_elapsed_milestone() { using var activity = _logger.StartActivity("overlay show"); activity.LogElapsed("first frame composited"); }

      [XF] public void the_milestone_line_reports_the_milestone_and_how_far_into_the_activity_it_was_reached() => _logger.Captured[1].Template.Must().NotBeNull().Be("{milestone} (+{elapsedMs:F1}ms)");
      [XF] public void the_milestone_is_captured() => _logger.Captured[1].Values![0].Must().Be("first frame composited");
      [XF] public void the_line_is_tagged_with_the_activity() => _logger.Captured[1].ActivityName.Must().NotBeNull().Be("overlay show");
   }

   public class that_completes : When_logging_within_an_activity
   {
      public that_completes() { using(_logger.StartActivity("overlay show")) {} }

      [XF] public void a_started_and_a_completed_line_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_completed_line_reports_the_total_time() => _logger.Captured[1].Template.Must().NotBeNull().Be("activity completed in {elapsedMs:F1}ms");
   }

   public class that_fails : When_logging_within_an_activity
   {
      public that_fails() { using(var activity = _logger.StartActivity("overlay show")) { activity.Fail(new InvalidOperationException("boom")); } }

      [XF] public void a_started_and_a_failed_line_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_failure_line_reports_the_elapsed_time_and_the_exception_type() => _logger.Captured[1].Template.Must().NotBeNull().Be("activity failed after {elapsedMs:F1}ms: {failureType}");
      [XF] public void the_failing_exception_type_is_captured() => _logger.Captured[1].Values![1].Must().Be("InvalidOperationException");
   }

   public class on_a_path_that_lost_the_ambient_activity : When_logging_within_an_activity
   {
      readonly string? _whileLost;
      readonly string? _afterMakeCurrent;
      public on_a_path_that_lost_the_ambient_activity()
      {
         using var activity = _logger.StartActivity("overlay show");
         System.Diagnostics.Activity.Current = null;   // a later dispatcher turn that did not inherit the activity
         _logger.Debug().Log("on the lost path");
         _whileLost = _logger.Captured[^1].ActivityName;
         using(activity.MakeCurrent()) { _logger.Debug().Log("re-established"); }
         _afterMakeCurrent = _logger.Captured[^1].ActivityName;
      }

      [XF] public void a_line_logged_after_the_context_was_lost_is_untagged() => (_whileLost is null).Must().Be(true);
      [XF] public void make_current_re_establishes_the_activity_so_the_line_is_tagged_again() => _afterMakeCurrent.Must().NotBeNull().Be("overlay show");
   }

   public class that_runs_twice_under_the_same_name : When_logging_within_an_activity
   {
      public that_runs_twice_under_the_same_name()
      {
         using(_logger.StartActivity("overlay show")) {}
         using(_logger.StartActivity("overlay show")) {}
      }

      [XF] public void both_runs_share_the_name() => (_logger.Captured[0].ActivityName, _logger.Captured[2].ActivityName).Must().Be(("overlay show", "overlay show"));
      [XF] public void the_two_runs_have_distinct_ids() => (_logger.Captured[0].ActivityId != _logger.Captured[2].ActivityId).Must().Be(true);
   }

   public class that_is_disposed_more_than_once : When_logging_within_an_activity
   {
      public that_is_disposed_more_than_once()
      {
         var activity = _logger.StartActivity("overlay show");
         activity.Dispose();
         activity.Dispose();
      }

      [XF] public void completion_is_logged_only_once() => _logger.Captured.Count.Must().Be(2);
   }

   public class when_the_activity_level_is_disabled : When_logging_within_an_activity
   {
      readonly CapturingLogger _recorder = new();
      public when_the_activity_level_is_disabled() { using(_recorder.WithLogLevel(LogLevel.Warning).StartActivity("overlay show")) {} }

      [XF] public void no_meta_lines_are_logged() => _recorder.Captured.Must().BeEmpty();
   }
}
