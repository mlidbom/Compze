using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.Logging;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it

class ConsoleLogger : ILogger
{
   readonly Type _type;

   LogLevel? _configuredLogLevel;

   LogLevel LogLevel => _configuredLogLevel ?? CompzeLogger.LogLevel;

   ConsoleLogger(Type type) => _type = type;

   public static ILogger Create(Type type) => new ConsoleLogger(type);
   public ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type) { _configuredLogLevel = level };

   public unit Error(Exception exception, string? message)
   {
      if(LogLevel >= LogLevel.Error)
      {
         ConsoleCE.WriteLine(ExceptionMessageBuilder.BuildExceptionLogMessage(exception, _type, message));
      }

      return unit.Value;
   }

   public unit Warning(string message)
   {
      if(LogLevel >= LogLevel.Warning)
      {
         ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return unit.Value;
   }

   public unit Warning(Exception exception, string message)
   {
      if(LogLevel >= LogLevel.Warning)
      {
         ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}, \n: Exception: {exception}");
      }

      return unit.Value;
   }

   public unit Info(string message)
   {
      if(LogLevel >= LogLevel.Info)
      {
         ConsoleCE.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return unit.Value;
   }

   public unit Debug(string message)
   {
      if(LogLevel >= LogLevel.Debug)
      {
         ConsoleCE.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return unit.Value;
   }
}
