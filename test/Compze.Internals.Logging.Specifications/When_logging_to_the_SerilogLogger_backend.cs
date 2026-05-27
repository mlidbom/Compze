using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Compze.Internals.Logging.ILogger;
using SerilogLogger = Compze.Internals.Logging.Serilog.SerilogLogger;

// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

public class When_logging_to_the_SerilogLogger_backend
{
   public sealed class InMemorySerilogSink : ILogEventSink
   {
      public List<LogEvent> Events { get; } = [];
      public void Emit(LogEvent logEvent) => Events.Add(logEvent);
   }

   protected readonly InMemorySerilogSink Sink = new();
   protected readonly ILogger Logger;
   protected When_logging_to_the_SerilogLogger_backend()
   {
      var serilog = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(Sink).CreateLogger();
      Logger = SerilogLogger.Create(GetType(), serilog).WithLogLevel(LogLevel.Debug);
   }

   public class an_interpolated_message_with_named_holes : When_logging_to_the_SerilogLogger_backend
   {
      public an_interpolated_message_with_named_holes()
      {
         var userId = 42;
         var action = "login";
         Logger.Info($"user {userId} did {action}");
      }

      [XF] public void Serilog_receives_the_original_template_text_with_named_holes()
         => Sink.Events[0].MessageTemplate.Text.Must().Be("user {userId} did {action}");

      [XF] public void the_first_hole_becomes_a_structured_property_named_userId()
         => ((ScalarValue)Sink.Events[0].Properties["userId"]).Value.Must().Be(42);

      [XF] public void the_second_hole_becomes_a_structured_property_named_action()
         => ((ScalarValue)Sink.Events[0].Properties["action"]).Value.Must().Be("login");
   }

   public class a_plain_string_message_containing_braces : When_logging_to_the_SerilogLogger_backend
   {
      public a_plain_string_message_containing_braces() => Logger.Info("literal {not a hole}");

      [XF] public void Serilog_receives_the_braces_escaped_so_it_does_not_attempt_to_parse_a_hole()
         => Sink.Events[0].MessageTemplate.Text.Must().Be("literal {{not a hole}}");

      [XF] public void no_hole_derived_property_is_attached_because_the_braces_were_escaped()
         => Sink.Events[0].Properties.Keys.Where(k => k is "not" or "a" or "hole").Must().BeEmpty();
   }

   public class an_interpolated_message_at_each_level : When_logging_to_the_SerilogLogger_backend
   {
      public an_interpolated_message_at_each_level()
      {
         var x = 1; Logger.Debug($"d {x}");
         var y = 2; Logger.Info($"i {y}");
         var z = 3; Logger.Warning($"w {z}");
         var q = 4; Logger.Warning(new InvalidOperationException("warn-ex"), $"we {q}");
         var r = 5; Logger.Error(new InvalidOperationException("err-ex"), $"e {r}");
      }

      [XF] public void each_level_arrives_at_Serilog_with_its_own_level()
         => Sink.Events.Select(it => it.Level).Must().SequenceEqual([LogEventLevel.Debug, LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Warning, LogEventLevel.Error]);

      [XF] public void each_template_carries_the_original_named_holes()
         => Sink.Events.Select(it => it.MessageTemplate.Text)
                       .Must().SequenceEqual(["d {x}", "i {y}", "w {z}", "we {q}", "e {r}"]);

      [XF] public void exceptions_passed_to_Warning_and_Error_are_attached_to_the_corresponding_log_events()
         => Sink.Events.Select(it => it.Exception?.Message)
                       .Must().SequenceEqual([null, null, null, "warn-ex", "err-ex"]);
   }
}
