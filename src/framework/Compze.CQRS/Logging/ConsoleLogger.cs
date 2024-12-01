using System;
using Compze.Functional;

namespace Compze.Logging;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it

enum LogLevel
{
   None = 0,
   Error = 1,
   Warning = 2,
   Info = 3,
   Debug = 4
}

class ConsoleLogger : ILogger
{
   readonly Type _type;

   LogLevel _logLevel = LogLevel.Info;

   ConsoleLogger(Type type) => _type = type;

   public static ILogger Create(Type type) => new ConsoleLogger(type);
   public ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type) { _logLevel = level };

   public Unit Error(Exception exception, string? message)
   {
      if(_logLevel >= LogLevel.Error)
      {
         ConsoleCE.WriteLine(ExceptionMessageBuilder.BuildExceptionLogMessage(exception, _type, message));
      }

      return Unit.Instance;
   }

   public Unit Warning(string message)
   {
      if(_logLevel >= LogLevel.Warning)
      {
         ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return Unit.Instance;
   }

   public Unit Warning(Exception exception, string message)
   {
      if(_logLevel >= LogLevel.Warning)
      {
         ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}, \n: Exception: {exception}");
      }

      return Unit.Instance;
   }

   public Unit Info(string message)
   {
      if(_logLevel >= LogLevel.Info)
      {
         ConsoleCE.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return Unit.Instance;
   }

   public Unit Debug(string message)
   {
      if(_logLevel >= LogLevel.Debug)
      {
         ConsoleCE.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return Unit.Instance;
   }
}
