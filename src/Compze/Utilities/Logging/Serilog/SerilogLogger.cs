using System;
using Compze.Utilities.Functional;
using Serilog;

#pragma warning disable CA2254 //We are implementing a wrapper around the logger, so complaining that we are not using a constant expression is not helpful

namespace Compze.Utilities.Logging.Serilog;

class SerilogLogger(global::Serilog.ILogger logger) : ILogger
{
   readonly global::Serilog.ILogger _logger = logger;
   LogLevel _logLevel = LogLevel.Warning;
   public static ILogger Create(Type type) => new SerilogLogger(Log.ForContext(type));

   public ILogger WithLogLevel(LogLevel level) => new SerilogLogger(_logger) {_logLevel =  level};

   public unit Error(Exception exception, string? message = null) => unit.From(() =>
   {
      if(_logLevel >= LogLevel.Error)
      {
         _logger.Error(exception, message ?? exception.GetType().FullName ?? "");
      }
   });

   public unit Warning(string message) => unit.From(() => unit.From(() =>
   {
      if(_logLevel >= LogLevel.Warning)
      {
         _logger.Warning(message);
      }
   }));

   public unit Warning(Exception exception, string message) => unit.From(() => unit.From(() =>
   {
      if(_logLevel >= LogLevel.Warning)
      {
         _logger.Warning(exception, message);
      }
   }));

   public unit Info(string message) => unit.From(() => unit.From(() =>
   {
      if(_logLevel >= LogLevel.Info)
      {
         _logger.Information(message);
      }
   }));

   public unit Debug(string message) => unit.From(() => unit.From(() =>
   {
      if(_logLevel >= LogLevel.Debug)
      {
         _logger.Debug(message);
      }
   }));
}
