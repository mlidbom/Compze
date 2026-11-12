using Microsoft.Diagnostics.NETCore.Client;

namespace Compze.Tests.Infrastructure.Diagnostics;

///<summary>A memory dump of the running test process, written while the process keeps running: what every thread and every<br/>
/// pending async operation was doing at one moment. The evidence for a failure that only happens where no debugger is, such as<br/>
/// an intermittent CI timeout: <c>dotnet-dump analyze</c> on the dump shows each thread's stack (<c>clrstack -all</c>) and<br/>
/// the await each pending async operation is suspended at (<c>dumpasync</c>).</summary>
///<remarks>Dumps go to the folder named by the <see cref="DiagnosticsDirectoryVariable"/> environment variable — CI points it<br/>
/// into the test results folder it uploads when a job fails — or else to a folder under the temp folder.</remarks>
public static class TestProcessDump
{
   ///<summary>The environment variable naming the folder dumps are written to.</summary>
   public const string DiagnosticsDirectoryVariable = "COMPZE_TEST_DIAGNOSTICS_DIRECTORY";

   ///<summary>Writes a dump of this process, heap included, and returns the dump file. The start of <paramref name="whatHappened"/><br/>
   /// names the file, so a folder of dumps tells which failure each belongs to — only the start, since createdump refuses<br/>
   /// a path longer than Windows' 260 characters.</summary>
   public static FileInfo Write(string whatHappened)
   {
      var directory = Directory.CreateDirectory(Environment.GetEnvironmentVariable(DiagnosticsDirectoryVariable)
                                             ?? Path.Combine(Path.GetTempPath(), "Compze", "Tests", "Diagnostics"));
      var dumpFile = new FileInfo(Path.Combine(directory.FullName, $"{FileNameSafe(whatHappened)}.pid{Environment.ProcessId}.{DateTime.UtcNow:yyyyMMddTHHmmssfff}.dmp"));
      new DiagnosticsClient(Environment.ProcessId).WriteDump(DumpType.WithHeap, dumpFile.FullName);
      return dumpFile;
   }

   static string FileNameSafe(string text) => new([..text.Take(40).Select(character => char.IsLetterOrDigit(character) ? character : '_')]);
}
