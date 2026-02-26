using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

class XUnitTestOutputHelperSink : ILogEventSink
{
   static readonly MessageTemplateTextFormatter Formatter = new("[{Timestamp:HH:mm:ss.fff} {Level:u3}] {LoggingClass}.{CallerMember}: {Message}{NewLine}{Exception}");

   public void Emit(LogEvent logEvent)
   {
      var testOutputHelper = TestContext.Current.TestOutputHelper;
      if(testOutputHelper == null) return;

      using var writer = new StringWriter();
      Formatter.Format(logEvent, writer);
      testOutputHelper.WriteLine(writer.ToString().TrimEnd());
   }
}
