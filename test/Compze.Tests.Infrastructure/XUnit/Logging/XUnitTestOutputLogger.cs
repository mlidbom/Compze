using System;
using Compze.Utilities.Logging;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

class XUnitTestOutputLogger : Logger
{
   readonly string _typeName;
   XUnitTestOutputLogger(Type type) => _typeName = type.Name;
   XUnitTestOutputLogger(Type type, LogLevel level) : base(level) => _typeName = type.Name;

   public static ILogger Create(Type type) => new XUnitTestOutputLogger(type);
   public override ILogger WithLogLevel(LogLevel level) => new XUnitTestOutputLogger(typeof(object), level);

   static void WriteLine(string message) => TestContext.Current.TestOutputHelper?.WriteLine(message);

   protected override void ErrorInternal(Exception exception, string? message) =>
      WriteLine($"ERROR:{_typeName}: {DateTime.Now:HH:mm:ss.fff} {message}\n{exception}");

   protected override void WarningInternal(string message) =>
      WriteLine($"WARN:{_typeName}: {DateTime.Now:HH:mm:ss.fff} {message}");

   protected override void WarningInternal(Exception exception, string message) =>
      WriteLine($"WARN:{_typeName}: {DateTime.Now:HH:mm:ss.fff} {message}\n{exception}");

   protected override void InfoInternal(string message) =>
      WriteLine($"INFO:{_typeName}: {DateTime.Now:HH:mm:ss.fff} {message}");

   protected override void DebugInternal(string message) =>
      WriteLine($"DEBUG:{_typeName}: {DateTime.Now:HH:mm:ss.fff} {message}");
}
