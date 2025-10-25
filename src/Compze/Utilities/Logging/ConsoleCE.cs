using System;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.ResourceAccess;

namespace Compze.Utilities.Logging;

static class ConsoleCE
{
   //NSpec breaks System.Console somehow when tests run in parallel. We are forced to synchronize these tests with other tests and this is the current workaround.
   static readonly MonitorCE Monitor = MonitorCE.WithTimeout(2.Seconds()); //We are just writing to the console. If this blocks for more than a second or so something is very wrong;

   internal static void WriteWarningLine(string tessage) => WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {tessage}");
   internal static void WriteImportantLine(string tessage) => WriteLine($"############################## {tessage}");
   internal static void WriteLine(string tessage) => Monitor.Update(() => Console.WriteLine(tessage));

   internal static void WriteLine() =>
      WriteLine("");
}