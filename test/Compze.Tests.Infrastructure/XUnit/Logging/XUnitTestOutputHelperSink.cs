using System.IO;
using Compze.Utilities.SystemCE;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

class XUnitTestOutputHelperSink : ILogEventSink
{
   static readonly MessageTemplateTextFormatter Formatter = CompzeEnvironment.IsGithubAction
      ? new("{Level:u3} {LogSource} ### {Message}{NewLine}{Exception}")
      : new("{Timestamp:HH:mm:ss.fff} {Level:u3} {LogSource} ### {Message}{NewLine}{Exception}");

   public void Emit(LogEvent logEvent)
   {
      var testOutputHelper = TestContext.Current.TestOutputHelper;
      if(testOutputHelper == null) return;

      using var writer = new StringWriter();
      Formatter.Format(logEvent, writer);
      testOutputHelper.WriteLine(writer.ToString().TrimEnd());
   }
}
