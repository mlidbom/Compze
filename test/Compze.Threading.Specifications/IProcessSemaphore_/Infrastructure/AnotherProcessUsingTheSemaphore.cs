using System.Globalization;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Interprocess;

namespace Compze.Threading.Specifications.IProcessSemaphore_.Infrastructure;

///<summary>Another process, holding units of an <see cref="IProcessSemaphore"/>, or waiting for them, until it is killed: a scenario of<br/>
/// <see cref="ProcessSemaphoreScenariosForAChildProcess"/>, in a process of its own. Disposing it kills the process.</summary>
sealed class AnotherProcessUsingTheSemaphore : IAsyncDisposable
{
   readonly ExplicitTestInAChildProcess _process;

   AnotherProcessUsingTheSemaphore(ExplicitTestInAChildProcess process) => _process = process;

   ///<summary>Starts the process, and returns once it holds <paramref name="units"/> units of <paramref name="semaphore"/>, the semaphore named<br/>
   /// <paramref name="name"/> in <paramref name="directory"/>.</summary>
   internal static async Task<AnotherProcessUsingTheSemaphore> HoldingAsync(int units, IProcessSemaphore semaphore, string name, DirectoryInfo directory, TimeSpan patience)
   {
      var holdingFile = Path.Combine(directory.FullName, $"{name}.held-by-another-process");
      var otherProcess = Start(nameof(ProcessSemaphoreScenariosForAChildProcess.take_the_units_and_hold_them_until_killed), units, semaphore, name, directory,
                               new Dictionary<string, string> { [ProcessSemaphoreScenariosForAChildProcess.HoldingFileVariable] = holdingFile });
      return await otherProcess.OnceAsync(() => File.Exists(holdingFile), "hold the units", patience);
   }

   ///<summary>Starts the process, and returns once it waits for <paramref name="units"/> units of <paramref name="semaphore"/>, the semaphore named<br/>
   /// <paramref name="name"/> in <paramref name="directory"/>: once the semaphore describes it among those who wait.</summary>
   internal static async Task<AnotherProcessUsingTheSemaphore> WaitingAsync(int units, IProcessSemaphore semaphore, string name, DirectoryInfo directory, TimeSpan patience)
   {
      var otherProcess = Start(nameof(ProcessSemaphoreScenariosForAChildProcess.wait_for_the_units_until_killed), units, semaphore, name, directory, new Dictionary<string, string>());
      return await otherProcess.OnceAsync(() => semaphore.DescribeHoldersAndWaiters().Contains(ProcessSemaphoreScenariosForAChildProcess.WaitingPurpose, StringComparison.Ordinal),
                                          "wait for the units", patience);
   }

   internal int ProcessId => _process.ProcessId;

   ///<summary>Kills the process, and returns once it has exited.</summary>
   internal Task KillAsync() => _process.KillAsync();

   public ValueTask DisposeAsync() => _process.DisposeAsync();

   static AnotherProcessUsingTheSemaphore Start(string scenario, int units, IProcessSemaphore semaphore, string name, DirectoryInfo directory, Dictionary<string, string> moreVariables)
   {
      moreVariables[ProcessSemaphoreScenariosForAChildProcess.SemaphoreNameVariable] = name;
      moreVariables[ProcessSemaphoreScenariosForAChildProcess.SemaphoreDirectoryVariable] = directory.FullName;
      moreVariables[ProcessSemaphoreScenariosForAChildProcess.UnitsOfTheSemaphoreVariable] = semaphore.Units.ToString(CultureInfo.InvariantCulture);
      moreVariables[ProcessSemaphoreScenariosForAChildProcess.UnitsToTakeVariable] = units.ToString(CultureInfo.InvariantCulture);
      return new AnotherProcessUsingTheSemaphore(ExplicitTestInAChildProcess.Start(typeof(ProcessSemaphoreScenariosForAChildProcess), scenario, moreVariables));
   }

   ///<summary>Returns this once <paramref name="itHasDoneIt"/>, which is <paramref name="whatItWasToDo"/>: or kills the process, and throws, if the process<br/>
   /// exits first, or hasn't done it within <paramref name="patience"/>.</summary>
   async Task<AnotherProcessUsingTheSemaphore> OnceAsync(Func<bool> itHasDoneIt, string whatItWasToDo, TimeSpan patience)
   {
      try
      {
         var waitEndsAt = DateTime.UtcNow + patience;
         while(!itHasDoneIt())
         {
            if(_process.HasExited) throw new InvalidOperationException($"The process that was to {whatItWasToDo} exited:{Environment.NewLine}{await _process.OutputOnceItExitsAsync(patience)}");
            if(DateTime.UtcNow > waitEndsAt) throw new TimeoutException($"The process that was to {whatItWasToDo} didn't within {patience}:{Environment.NewLine}{await _process.KillAndReadTheOutputAsync()}");
            await Task.Delay(20);
         }

         return this;
      }
      catch //Cleanup, not handling: a process that never did what it was to do must not outlive the specification.
      {
         await DisposeAsync();
         throw;
      }
   }
}
