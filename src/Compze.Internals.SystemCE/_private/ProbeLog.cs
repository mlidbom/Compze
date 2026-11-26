namespace Compze.Internals.SystemCE._private;

///<summary>PROBE, for the flake hunt only: writes what a probe found to probe.log in the test diagnostics directory, and to stderr.</summary>
public static class ProbeLog
{
   static readonly Lock WriteLock = new();

   public static void Write(string message)
   {
      var line = $"{DateTime.UtcNow:O} [pid {Environment.ProcessId}, thread {Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}";
      lock(WriteLock)
      {
         Console.Error.Write(line);
         var directory = Environment.GetEnvironmentVariable("COMPZE_TEST_DIAGNOSTICS_DIRECTORY");
         if(string.IsNullOrEmpty(directory)) return;
         Directory.CreateDirectory(directory);
         File.AppendAllText(Path.Combine(directory, "probe.log"), line);
      }
   }
}
