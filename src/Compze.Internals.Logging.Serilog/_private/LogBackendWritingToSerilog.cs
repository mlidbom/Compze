using System.Diagnostics;
using Serilog.Events;

// We are a wrapper around Serilog. CA2254 ("message template must be constant") cannot be honored
// at this layer — the templates are constructed by call sites and forwarded here. CA2254 is meant
// to ensure the template-cache key is stable; in our case the template string IS the cache key
// (built by the interpolated string handler), so the cache still works correctly.
#pragma warning disable CA2254

namespace Compze.Internals.Logging.Serilog._private;

///<summary>
/// The <see cref="ILogBackend"/> <see cref="SerilogLogBackend"/> hands out: writes each <see cref="LogLine"/> to the Serilog
/// logger its source returns at that moment, as a structured event.
///</summary>
///<remarks>
/// Each event carries the line's <see cref="LogLine.SourceType"/> as Serilog's <c>SourceContext</c>, its
/// <see cref="LogLine.CallerMember"/> as <c>CallerMember</c>, and the ambient <see cref="Activity.Current"/>, when one is set,
/// as <c>Activity</c> (the operation name) and <c>ActivityId</c>.
///</remarks>
sealed class LogBackendWritingToSerilog : ILogBackend
{
   readonly Func<global::Serilog.ILogger> _currentSerilogLogger;

   internal LogBackendWritingToSerilog(Func<global::Serilog.ILogger> currentSerilogLogger) => _currentSerilogLogger = currentSerilogLogger;

   public void Write(LogLine line) =>
      // ReSharper disable once TemplateIsNotCompileTimeConstantProblem Compze forwards templates its callers built; the template string is Serilog's cache key, so caching still works (as for CA2254 above).
      SerilogLoggerFor(line).Write(SerilogLevelOf(line.Level), line.Exception, line.MessageTemplate, [.. line.TemplateValues]);

   global::Serilog.ILogger SerilogLoggerFor(LogLine line)
   {
      var logger = _currentSerilogLogger().ForContext(line.SourceType).ForContext("CallerMember", line.CallerMember);
      return Activity.Current is {} activity
                ? logger.ForContext("Activity", activity.OperationName).ForContext("ActivityId", activity.Id)
                : logger;
   }

   ///<remarks>Compze's <see cref="LogLevel.Trace"/> is Serilog's <see cref="LogEventLevel.Verbose"/>, and Compze's <see cref="LogLevel.Critical"/> is Serilog's <see cref="LogEventLevel.Fatal"/>: the two frameworks name their extreme levels differently.</remarks>
   static LogEventLevel SerilogLevelOf(LogLevel level) => level switch
   {
      LogLevel.Trace    => LogEventLevel.Verbose,
      LogLevel.Debug    => LogEventLevel.Debug,
      LogLevel.Info     => LogEventLevel.Information,
      LogLevel.Warning  => LogEventLevel.Warning,
      LogLevel.Error    => LogEventLevel.Error,
      LogLevel.Critical => LogEventLevel.Fatal,
      _                 => throw new ArgumentOutOfRangeException(nameof(level), level, "Not a LogLevel member.")
   };
}
