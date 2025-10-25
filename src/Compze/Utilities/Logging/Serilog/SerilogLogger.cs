using System;
using Serilog;

#pragma warning disable CA2254 //We are implementing a wrapper around the logger, so complaining that we are not using a constant expression is not helpful

namespace Compze.Utilities.Logging.Serilog;

class SerilogLogger : Logger
{
   readonly global::Serilog.ILogger _logger;
   SerilogLogger(global::Serilog.ILogger logger) => _logger = logger;
   SerilogLogger(global::Serilog.ILogger logger, LogLevel level) : base(level) => _logger = logger;

   public static ILogger Create(Type type) => new SerilogLogger(Log.ForContext(type));

   public override ILogger WithLogLevel(LogLevel level) =>
      new SerilogLogger(_logger, level);

   protected override void ErrorInternal(Exception exception, string? tessage) =>
      _logger.Error(exception, tessage ?? exception.GetType().FullName ?? "");

   protected override void WarningInternal(string tessage) =>
      _logger.Warning(tessage);

   protected override void WarningInternal(Exception exception, string tessage) =>
      _logger.Warning(exception, tessage);

   protected override void InfoInternal(string tessage) =>
      _logger.Information(tessage);

   protected override void DebugInternal(string tessage) =>
      _logger.Debug(tessage);
}
