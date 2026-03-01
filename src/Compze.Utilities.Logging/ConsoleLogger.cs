using System;

namespace Compze.Utilities.Logging;

internal class ConsoleLogger : Logger
{
   readonly Type _type;
   ConsoleLogger(Type type) => _type = type;
   ConsoleLogger(Type type, LogLevel level) : base(level) => _type = type;

   public static ILogger Create(Type type) => new ConsoleLogger(type);
   public override ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type, level);

   protected override void ErrorInternal(Exception exception, string? message, string caller) =>
      ConsoleCE.WriteLine(ExceptionTessageBuilder.BuildExceptionLogTessage(exception, _type, caller, message));

   protected override void WarningInternal(string message, string caller) =>
      ConsoleCE.WriteLine($"{DateTime.Now:HH:mm:ss.fff} WRN {LogSourceFormatter.Format(_type.Name, caller)} ### {message}");

   protected override void WarningInternal(Exception exception, string message, string caller) =>
      ConsoleCE.WriteLine($"{DateTime.Now:HH:mm:ss.fff} WRN {LogSourceFormatter.Format(_type.Name, caller)} ### {message}, \n: Exception: {exception}");

   protected override void InfoInternal(string message, string caller) => 
      ConsoleCE.WriteLine($"{DateTime.Now:HH:mm:ss.fff} INF {LogSourceFormatter.Format(_type.Name, caller)} ### {message}");

   protected override void DebugInternal(string message, string caller) =>
      ConsoleCE.WriteLine($"{DateTime.Now:HH:mm:ss.fff} DBG {LogSourceFormatter.Format(_type.Name, caller)} ### {message}");
}
