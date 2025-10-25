using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

abstract class Logger : ILogger
{
   readonly LogLevel? _configuredLogLevel;

   LogLevel LogLevel => _configuredLogLevel ?? CompzeLogger.LogLevel;

   protected Logger() {}

   protected Logger(LogLevel logLevel) => _configuredLogLevel = logLevel;

   protected abstract void ErrorInternal(Exception exception, string? message);
   public abstract ILogger WithLogLevel(LogLevel level);

   public unit Error(Exception exception, string? message) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Error)
      {
         ErrorInternal(exception, message);
      }
   });

   protected abstract void WarningInternal(string message);

   public unit Warning(string message) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Warning)
      {
         WarningInternal(message);
      }
   });

   protected abstract void WarningInternal(Exception exception, string message);

   public unit Warning(Exception exception, string message) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Warning)
      {
         WarningInternal(exception, message);
      }
   });

   protected abstract void InfoInternal(string message);

   public unit Info(string message) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Info)
      {
         InfoInternal(message);
      }
   });

   protected abstract void DebugInternal(string message);

   public unit Debug(string message) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Debug)
      {
         DebugInternal(message);
      }
   });
}
