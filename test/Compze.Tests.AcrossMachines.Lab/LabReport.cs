using System.Diagnostics;
using System.Globalization;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>What the lab's program reports of a conversation: one line for each thing that happened, with the time it<br/>
/// happened at, on the console. Someone reads the lines; nothing asserts on them.</summary>
static class LabReport
{
   internal static void Line(string whatHappened) => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {whatHappened}");

   internal static string MillisecondsOf(Stopwatch stopwatch) => stopwatch.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture);
}
