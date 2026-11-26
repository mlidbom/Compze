using System.Diagnostics;

namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>What a holder of a lock between processes writes beside it for those who wait meanwhile: what it holds it for, which process it is, and<br/>
/// since when.</summary>
static class HoldersDescription
{
   ///<summary>Who takes a lock, as the description of its holder tells those who wait: this process, by its id and name, and the machine.</summary>
   static readonly string ThisProcess = DescribeThisProcess();

   ///<summary>This process, holding a lock from now on for <paramref name="purpose"/>: "for {purpose}, by process {id} ({name}) on {machine}, since<br/>
   /// {time}".</summary>
   internal static string OfThisProcessHoldingItFor(string purpose) => $"for {purpose}, by {ThisProcess}, since {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";

   static string DescribeThisProcess()
   {
      using var thisProcess = Process.GetCurrentProcess();
      return $"process {thisProcess.Id} ({thisProcess.ProcessName}) on {Environment.MachineName}";
   }
}
