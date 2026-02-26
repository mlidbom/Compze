using System;
using Serilog;

#pragma warning disable CA2254 //We are implementing a wrapper around the logger, so complaining that we are not using a constant expression is not helpful

namespace Compze.Utilities.Logging.Serilog;

public class SerilogLogger : Logger
{
   readonly global::Serilog.ILogger _logger;
   SerilogLogger(global::Serilog.ILogger logger) => _logger = logger;
   SerilogLogger(global::Serilog.ILogger logger, LogLevel level) : base(level) => _logger = logger;

   public static ILogger Create(Type type) => new SerilogLogger(Log.ForContext(type));

   public override ILogger WithLogLevel(LogLevel level) =>
      new SerilogLogger(_logger, level);

   protected override void ErrorInternal(Exception exception, string? message, string caller) =>
      _logger.Error(exception, $"{caller}: {message ?? exception.GetType().FullName ?? ""}");

   protected override void WarningInternal(string message, string caller) =>
      _logger.Warning($"{caller}: {message}");

   protected override void WarningInternal(Exception exception, string message, string caller) =>
      _logger.Warning(exception, $"{caller}: {message}");

   protected override void InfoInternal(string message, string caller) =>
      _logger.Information($"{caller}: {message}");

   protected override void DebugInternal(string message, string caller) =>
      _logger.Debug($"{caller}: {message}");
}
