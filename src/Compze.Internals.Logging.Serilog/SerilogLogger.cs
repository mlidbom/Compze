using System.Diagnostics;
using Compze.Internals.SystemCE;
using Serilog;

// We are a wrapper around Serilog. CA2254 ("message template must be constant") cannot be honored
// at this layer — the templates are constructed by call sites and forwarded here. CA2254 is meant
// to ensure the template-cache key is stable; in our case the template string IS the cache key
// (built by the interpolated string handler), so the cache still works correctly.
#pragma warning disable CA2254

namespace Compze.Internals.Logging.Serilog;

public class SerilogLogger : Logger
{
   readonly global::Serilog.ILogger _logger;
   SerilogLogger(global::Serilog.ILogger logger) => _logger = logger;
   SerilogLogger(global::Serilog.ILogger logger, LogLevel level) : base(level) => _logger = logger;

   public static ILogger Create(Type type) => new SerilogLogger(Log.ForContext(type));
   public static ILogger Create(Type type, global::Serilog.ILogger serilog) => new SerilogLogger(serilog.ForContext(type));

   public override ILogger WithLogLevel(LogLevel level) => new SerilogLogger(_logger, level);

   global::Serilog.ILogger CallerLogger(string caller)
   {
      var logger = _logger.ForContext("CallerMember", caller);
      return Activity.Current is {} activity
                ? logger.ForContext("Activity", activity.OperationName).ForContext("ActivityId", activity.Id)
                : logger;
   }

   protected override void ErrorInternal(Exception exception, string template, object?[]? values, string caller)
   {
      var logger = CallerLogger(caller);
      if(values == null)
      {
         logger.Error(exception, EscapeLiteral(template));
      } else
      {
         logger.Error(exception, template, values);
      }
   }

   protected override void WarningInternal(Exception? exception, string template, object?[]? values, string caller)
   {
      var logger = CallerLogger(caller);
      if(values == null)
      {
         if(exception == null) logger.Warning(EscapeLiteral(template));
         else logger.Warning(exception, EscapeLiteral(template));
      } else
      {
         if(exception == null) logger.Warning(template, values);
         else logger.Warning(exception, template, values);
      }
   }

   protected override void InfoInternal(string template, object?[]? values, string caller)
   {
      var logger = CallerLogger(caller);
      if(values == null) logger.Information(EscapeLiteral(template));
      else logger.Information(template, values);
   }

   protected override void DebugInternal(string template, object?[]? values, string caller)
   {
      var logger = CallerLogger(caller);
      if(values == null) logger.Debug(EscapeLiteral(template));
      else logger.Debug(template, values);
   }

   // For plain (non-handler) messages we treat the string as a literal, not a Serilog template.
   // Escape '{' / '}' so Serilog doesn't try to parse holes that aren't there.
   static string EscapeLiteral(string message) =>
      message.ContainsOrdinal('{') || message.ContainsOrdinal('}')
         ? message.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal)
         : message;
}
