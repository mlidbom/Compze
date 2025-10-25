using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

abstract class Logger : ILogger
{
   readonly LogLevel? _configuredLogLevel;

   LogLevel LogLevel => _configuredLogLevel ?? CompzeLogger.LogLevel;

   protected Logger() {}

   protected Logger(LogLevel logLevel) => _configuredLogLevel = logLevel;

   protected abstract void ErrorInternal(Exception exception, string? tessage);
   public abstract ILogger WithLogLevel(LogLevel level);

   public unit Error(Exception exception, string? tessage) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Error)
      {
         ErrorInternal(exception, tessage);
      }
   });

   protected abstract void WarningInternal(string tessage);

   public unit Warning(string tessage) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Warning)
      {
         WarningInternal(tessage);
      }
   });

   protected abstract void WarningInternal(Exception exception, string tessage);

   public unit Warning(Exception exception, string tessage) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Warning)
      {
         WarningInternal(exception, tessage);
      }
   });

   protected abstract void InfoInternal(string tessage);

   public unit Info(string tessage) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Info)
      {
         InfoInternal(tessage);
      }
   });

   protected abstract void DebugInternal(string tessage);

   public unit Debug(string tessage) => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Debug)
      {
         DebugInternal(tessage);
      }
   });
}
