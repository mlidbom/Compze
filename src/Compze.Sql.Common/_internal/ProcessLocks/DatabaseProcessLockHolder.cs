using System.Diagnostics;
using Compze.Contracts;

namespace Compze.Sql.Common._internal.ProcessLocks;

///<summary>How the holder of a lock of <see cref="IDatabaseProcessLocks"/> describes itself, in the holder record that<br/>
/// <see cref="IDatabaseProcessLocks.HolderOf"/> reads: what it holds the lock for, which process it is, on which machine, and since when.<br/>
/// The same form as Compze.Threading.Interprocess's <c>IProcessLock</c> gives its holders.</summary>
static class DatabaseProcessLockHolder
{
   static readonly string ThisProcess = DescribeThisProcess();

   ///<summary>This process, as the holder of a lock it takes now for <paramref name="purpose"/>.</summary>
   internal static string ThisProcessTakingItFor(string purpose)
   {
      Argument.Assert(!string.IsNullOrWhiteSpace(purpose), () => "Taking a database process lock takes a purpose: what the lock is to be held for, to those who are refused it meanwhile.");
      var description = $"for {purpose}, by {ThisProcess}, since {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";
      return description.Length <= DatabaseProcessLockHolderSchemaStrings.HolderLength ? description : description[..DatabaseProcessLockHolderSchemaStrings.HolderLength];
   }

   ///<summary>Asserts that <paramref name="lockName"/> can name a lock: the holder table's key holds it.</summary>
   internal static void AssertIsALockName(string lockName) =>
      Argument.Assert(!string.IsNullOrWhiteSpace(lockName) && lockName.Length <= DatabaseProcessLockHolderSchemaStrings.LockNameLength,
                      () => $"A database process lock's name must be at most {DatabaseProcessLockHolderSchemaStrings.LockNameLength} characters long: \"{lockName}\".");

   static string DescribeThisProcess()
   {
      using var thisProcess = Process.GetCurrentProcess();
      return $"process {thisProcess.Id} ({thisProcess.ProcessName}) on {Environment.MachineName}";
   }
}
