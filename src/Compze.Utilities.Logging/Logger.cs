using System;
using System.Runtime.CompilerServices;
using Compze.Underscore;

namespace Compze.Utilities.Logging;

public abstract class Logger : ILogger
{
   readonly LogLevel? _configuredLogLevel;

   LogLevel LogLevel => _configuredLogLevel ?? CompzeLogger.LogLevel;

   protected Logger() {}

   protected Logger(LogLevel logLevel) => _configuredLogLevel = logLevel;

   protected abstract void ErrorInternal(Exception exception, string? message, string caller);
   public abstract ILogger WithLogLevel(LogLevel level);

   public unit Error(Exception exception, string? message = null, [CallerMemberName] string caller = "") => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Error)
      {
         ErrorInternal(exception, message, caller);
      }
   });

   protected abstract void WarningInternal(string message, string caller);

   public unit Warning(string message, [CallerMemberName] string caller = "") => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Warning)
      {
         WarningInternal(message, caller);
      }
   });

   protected abstract void WarningInternal(Exception exception, string message, string caller);

   public unit Warning(Exception exception, string message, [CallerMemberName] string caller = "") => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Warning)
      {
         WarningInternal(exception, message, caller);
      }
   });

   protected abstract void InfoInternal(string message, string caller);

   public unit Info(string message, [CallerMemberName] string caller = "") => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Info)
      {
         InfoInternal(message, caller);
      }
   });

   protected abstract void DebugInternal(string message, string caller);

   public unit Debug(string message, [CallerMemberName] string caller = "") => unit.From(() =>
   {
      if(!CompzeLogger.LoggingSuppressed && LogLevel >= LogLevel.Debug)
      {
         DebugInternal(message, caller);
      }
   });
}
