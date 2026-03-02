using System;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Utilities.Logging;

public static class ConsoleCE
{
   //NSpec breaks System.Console somehow when tests run in parallel. We are forced to synchronize these tests with other tests and this is the current workaround.
   static readonly IMonitor MonitorCE = IMonitor.New(LockTimeout.Seconds(2)); //We are just writing to the console. If this blocks for more than a second or so something is very wrong;

   public static void WriteWarningLine(string message) => WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {message}");
   public static void WriteImportantLine(string message) => WriteLine($"############################## {message}");
   public static void WriteLine(string message) => MonitorCE.Locked(() => Console.WriteLine(message));

   public static void WriteLine() =>
      WriteLine("");
}