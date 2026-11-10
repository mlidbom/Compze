using Compze.Internals.Logging.Serilog;
using Serilog;
using static Compze.Internals.Logging.Specifications.When_logging_to_a_Serilog_backend;

// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

///<summary>
/// <see cref="SerilogLogBackend.Global"/> writes to Serilog's static <see cref="Log.Logger"/>. It used to capture
/// <see cref="Log.Logger"/> when a logger was made, so a logger made before the host assigned it wrote to Serilog's silent
/// default logger for good, and its lines were lost.
///</summary>
public sealed class When_the_global_Serilog_backend_is_chosen_before_Serilogs_static_logger_is_assigned : IDisposable
{
   readonly global::Serilog.ILogger _staticLoggerBefore = Log.Logger;
   readonly InMemorySerilogSink _sinkOfTheStaticLoggerAssignedLater = new();

   public When_the_global_Serilog_backend_is_chosen_before_Serilogs_static_logger_is_assigned()
   {
      CompzeLogger.Backend = SerilogLogBackend.Global;
      var logger = CompzeLogger.For<When_the_global_Serilog_backend_is_chosen_before_Serilogs_static_logger_is_assigned>().WithLogLevel(LogLevel.Debug);
      Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(_sinkOfTheStaticLoggerAssignedLater).CreateLogger();
      logger.Info("logged after the static logger was assigned");
   }

   [XF] public void a_line_logged_after_the_static_logger_is_assigned_reaches_that_logger()
      => _sinkOfTheStaticLoggerAssignedLater.Events.Select(it => it.MessageTemplate.Text).Must().SequenceEqual(["logged after the static logger was assigned"]);

   public void Dispose() => Log.Logger = _staticLoggerBefore;
}
