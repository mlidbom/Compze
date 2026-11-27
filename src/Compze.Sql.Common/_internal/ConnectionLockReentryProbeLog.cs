using System.Collections.Concurrent;

namespace Compze.Sql.Common._internal;

//PROBE, experiment only: writes each distinct stack of a re-entry once, per process.
static class ConnectionLockReentryProbeLog
{
   static readonly ConcurrentDictionary<string, int> SeenStacks = new();
   static readonly Lock FileLock = new();
   static readonly string Folder = Path.Combine(Path.GetTempPath(), "compze-connection-lock-reentry-probe");
   static readonly string FilePath = Path.Combine(Folder, $"{Environment.ProcessId}-{Guid.NewGuid():N}.log");

   internal static void Record(string kind)
   {
      var stack = Environment.StackTrace;
      var count = SeenStacks.AddOrUpdate(stack, 1, (_, it) => it + 1);
      if(count != 1) return;
      lock(FileLock)
      {
         Directory.CreateDirectory(Folder);
         File.AppendAllText(FilePath, $"=== {kind} re-entry, {DateTime.Now:O}, process {Environment.ProcessId}\n{stack}\n\n");
      }
   }
}
