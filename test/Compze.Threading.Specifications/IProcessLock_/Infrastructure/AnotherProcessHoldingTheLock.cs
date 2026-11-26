using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Interprocess;

namespace Compze.Threading.Specifications.IProcessLock_.Infrastructure;

///<summary>Another process, holding an <see cref="IProcessLock"/> until it is killed: <see cref="ProcessLockScenariosForAChildProcess.take_the_lock_and_hold_it_until_killed"/>,<br/>
/// in a process of its own. Disposing it kills the process.</summary>
sealed class AnotherProcessHoldingTheLock : IAsyncDisposable
{
   readonly ExplicitTestInAChildProcess _process;

   AnotherProcessHoldingTheLock(ExplicitTestInAChildProcess process) => _process = process;

   ///<summary>Starts the process, and returns once it holds the lock named <paramref name="lockName"/> in <paramref name="lockDirectory"/>.</summary>
   internal static async Task<AnotherProcessHoldingTheLock> StartAsync(string lockName, DirectoryInfo lockDirectory, TimeSpan patience)
   {
      var holdingFile = Path.Combine(lockDirectory.FullName, $"{lockName}.held-by-another-process");
      var process = ExplicitTestInAChildProcess.Start(typeof(ProcessLockScenariosForAChildProcess),
                                                      nameof(ProcessLockScenariosForAChildProcess.take_the_lock_and_hold_it_until_killed),
                                                      new Dictionary<string, string>
                                                      {
                                                         [ProcessLockScenariosForAChildProcess.LockNameVariable] = lockName,
                                                         [ProcessLockScenariosForAChildProcess.LockDirectoryVariable] = lockDirectory.FullName,
                                                         [ProcessLockScenariosForAChildProcess.HoldingFileVariable] = holdingFile
                                                      });
      var otherProcess = new AnotherProcessHoldingTheLock(process);
      try
      {
         await otherProcess.AwaitItHoldingTheLockAsync(holdingFile, patience);
         return otherProcess;
      }
      catch //Cleanup, not handling: a process that never came to hold the lock must not outlive the specification.
      {
         await otherProcess.DisposeAsync();
         throw;
      }
   }

   internal int ProcessId => _process.ProcessId;

   ///<summary>Kills the process, and returns once it has exited.</summary>
   internal Task KillAsync() => _process.KillAsync();

   async Task AwaitItHoldingTheLockAsync(string holdingFile, TimeSpan patience)
   {
      var waitEndsAt = DateTime.UtcNow + patience;
      while(!File.Exists(holdingFile))
      {
         if(_process.HasExited) throw new InvalidOperationException($"The process that was to hold the lock exited:{Environment.NewLine}{await _process.OutputOnceItExitsAsync(patience)}");
         if(DateTime.UtcNow > waitEndsAt) throw new TimeoutException($"The process that was to hold the lock didn't take it within {patience}:{Environment.NewLine}{await _process.OutputOnceItExitsAsync(TimeSpan.Zero)}");
         await Task.Delay(20);
      }
   }

   public ValueTask DisposeAsync() => _process.DisposeAsync();
}
