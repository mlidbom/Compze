using System;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Logging;

static class ConsoleCE
{
   //NSpec breaks System.Console somehow when tests run in parallel. We are forced to synchronize these tests with other tests and this is the current workaround.
   static readonly MonitorCE Monitor = MonitorCE.WithTimeout(2.Seconds()); //We are just writing to the console. If this blocks for more than a second or so something is very wrong;

   internal static void WriteWarningLine(string message) => WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {message}");
   internal static void WriteImportantLine(string message) => WriteLine($"############################## {message}");
   internal static void WriteLine(string message) => Monitor.Update(() => Console.WriteLine(message));

   internal static void WriteLine() =>
      WriteLine("");
}