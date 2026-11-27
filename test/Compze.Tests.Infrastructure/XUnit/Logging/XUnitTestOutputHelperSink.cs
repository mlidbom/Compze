using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit.Logging;

class XUnitTestOutputHelperSink : ILogEventSink
{
   ///<summary>Timestamped in CI too: a test's output reaches the job log in one piece once the test has ended, so the time<br/>
   /// GitHub Actions stamps on each line is when the test ended, the same for every line, never when it was logged.</summary>
   static readonly MessageTemplateTextFormatter Formatter = new("{Timestamp:HH:mm:ss.fff} {Level:u3} {LogSource} ### {Message}{NewLine}{Exception}");

   public void Emit(LogEvent logEvent)
   {
      var testOutputHelper = TestContext.Current.TestOutputHelper;
      if(testOutputHelper == null) return;

      using var writer = new StringWriter();
      Formatter.Format(logEvent, writer);
      testOutputHelper.WriteLine(writer.ToString().TrimEnd());
   }
}
