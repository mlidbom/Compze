// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

public class When_logging_through_a_flow_log
{
   readonly CapturingLogger _logger = new();

   static bool IsTaggedWithFlow(CapturedLogCall call, string flowName) => call.Properties.Any(property => property.Name == "Flow" && Equals(property.Value, flowName));

   public class that_has_just_been_started : When_logging_through_a_flow_log
   {
      public that_has_just_been_started() => _logger.StartFlowLogger("overlay show");

      [XF] public void a_started_line_is_logged() => _logger.Captured.Count.Must().Be(1);
      [XF] public void the_started_line_announces_the_start() => _logger.Captured[0].Template.Must().NotBeNull().Be("flow started");
      [XF] public void the_line_is_tagged_with_the_flow_name() => IsTaggedWithFlow(_logger.Captured[0], "overlay show").Must().Be(true);
   }

   public class that_logs_a_message : When_logging_through_a_flow_log
   {
      public that_logs_a_message() => _logger.StartFlowLogger("overlay show").Log("preparing surfaces");

      [XF] public void the_message_is_logged() => _logger.Captured[1].Template.Must().NotBeNull().Be("preparing surfaces");
      [XF] public void the_message_is_tagged_with_the_flow_name() => IsTaggedWithFlow(_logger.Captured[1], "overlay show").Must().Be(true);
   }

   public class that_logs_an_elapsed_milestone : When_logging_through_a_flow_log
   {
      public that_logs_an_elapsed_milestone() => _logger.StartFlowLogger("overlay show").LogElapsed("first frame composited");

      [XF] public void the_milestone_line_reports_the_milestone_and_how_far_into_the_flow_it_was_reached() => _logger.Captured[1].Template.Must().NotBeNull().Be("{milestone} (+{elapsedMs:F1}ms)");
      [XF] public void the_milestone_is_captured() => _logger.Captured[1].Values![0].Must().Be("first frame composited");
      [XF] public void the_elapsed_time_is_captured_as_a_structured_double() => (_logger.Captured[1].Values![1] is double).Must().Be(true);
      [XF] public void the_line_is_tagged_with_the_flow_name() => IsTaggedWithFlow(_logger.Captured[1], "overlay show").Must().Be(true);
   }

   public class that_times_an_operation_within_the_flow : When_logging_through_a_flow_log
   {
      public that_times_an_operation_within_the_flow() => _logger.StartFlowLogger("overlay show").Time(() => 42);

      [XF] public void the_span_start_line_is_tagged_with_the_flow_name() => IsTaggedWithFlow(_logger.Captured[1], "overlay show").Must().Be(true);
      [XF] public void the_span_completion_line_is_tagged_with_the_flow_name() => IsTaggedWithFlow(_logger.Captured[2], "overlay show").Must().Be(true);
   }

   public class that_is_disposed : When_logging_through_a_flow_log
   {
      public that_is_disposed() { using(_logger.StartFlowLogger("overlay show")) {} }

      [XF] public void a_started_and_a_completed_line_are_logged() => _logger.Captured.Count.Must().Be(2);
      [XF] public void the_completed_line_reports_the_total_time() => _logger.Captured[1].Template.Must().NotBeNull().Be("flow completed in {elapsedMs:F1}ms");
      [XF] public void the_completed_line_is_tagged_with_the_flow_name() => IsTaggedWithFlow(_logger.Captured[1], "overlay show").Must().Be(true);
   }

   public class that_is_disposed_more_than_once : When_logging_through_a_flow_log
   {
      public that_is_disposed_more_than_once()
      {
         var flow = _logger.StartFlowLogger("overlay show");
         flow.Dispose();
         flow.Dispose();
      }

      [XF] public void completion_is_logged_only_once() => _logger.Captured.Count.Must().Be(2);
   }

   public class when_the_flow_level_is_disabled : When_logging_through_a_flow_log
   {
      readonly CapturingLogger _recorder = new();
      public when_the_flow_level_is_disabled() => _recorder.WithLogLevel(LogLevel.Warning).StartFlowLogger("overlay show").LogElapsed("first frame");

      [XF] public void no_lines_are_logged() => _recorder.Captured.Must().BeEmpty();
   }
}
