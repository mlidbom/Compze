using System;
using Compze.Functional;
using Serilog;

namespace Compze.Logging.Serilog;

class SerilogLogger(global::Serilog.ILogger logger) : ILogger
{
   readonly global::Serilog.ILogger _logger = logger;
   LogLevel _logLevel = LogLevel.Info;
   public static ILogger Create(Type type) => new SerilogLogger(Log.ForContext(type));

   public ILogger WithLogLevel(LogLevel level) => new SerilogLogger(_logger) {_logLevel =  level};

   public Unit Error(Exception exception, string? message = null) => Unit.From(() =>
   {
      if(_logLevel >= LogLevel.Error)
      {
         _logger.Error(exception, message ?? exception.GetType().FullName ?? "");
      }
   });

   public Unit Warning(string message) => Unit.From(() => Unit.From(() =>
   {
      if(_logLevel >= LogLevel.Warning)
      {
         _logger.Warning(message);
      }
   }));

   public Unit Warning(Exception exception, string message) => Unit.From(() => Unit.From(() =>
   {
      if(_logLevel >= LogLevel.Warning)
      {
         _logger.Warning(exception, message);
      }
   }));

   public Unit Info(string message) => Unit.From(() => Unit.From(() =>
   {
      if(_logLevel >= LogLevel.Info)
      {
         _logger.Information(message);
      }
   }));

   public Unit Debug(string message) => Unit.From(() => Unit.From(() =>
   {
      if(_logLevel >= LogLevel.Debug)
      {
         _logger.Debug(message);
      }
   }));
}
