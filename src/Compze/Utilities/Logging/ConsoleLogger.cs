using System;

namespace Compze.Utilities.Logging;

class ConsoleLogger : Logger
{
   readonly Type _type;
   ConsoleLogger(Type type) => _type = type;
   ConsoleLogger(Type type, LogLevel level) : base(level) => _type = type;

   public static ILogger Create(Type type) => new ConsoleLogger(type);
   public override ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type, level);

   protected override void ErrorInternal(Exception exception, string? tessage) =>
      ConsoleCE.WriteLine(ExceptionTessageBuilder.BuildExceptionLogTessage(exception, _type, tessage));

   protected override void WarningInternal(string tessage) =>
      ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {tessage}");

   protected override void WarningInternal(Exception exception, string tessage) =>
      ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {tessage}, \n: Exception: {exception}");

   protected override void InfoInternal(string tessage) => 
      ConsoleCE.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {tessage}");

   protected override void DebugInternal(string tessage) =>
      ConsoleCE.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {tessage}");
}
