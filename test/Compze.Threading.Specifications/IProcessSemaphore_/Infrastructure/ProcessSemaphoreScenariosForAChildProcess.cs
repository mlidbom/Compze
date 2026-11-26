using System.Globalization;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_.Infrastructure;

///<summary>What the specifications of <see cref="IProcessSemaphore"/> have another process do with the semaphore, through<br/>
/// <see cref="Tests.Infrastructure.XUnit.ExplicitTestInAChildProcess"/>. Each is explicit, so that no ordinary test run runs it, and takes units of<br/>
/// the semaphore its environment variables name.</summary>
public class ProcessSemaphoreScenariosForAChildProcess
{
   internal const string SemaphoreNameVariable = "COMPZE_PROCESS_SEMAPHORE_SPECIFICATIONS_SEMAPHORE_NAME";
   internal const string SemaphoreDirectoryVariable = "COMPZE_PROCESS_SEMAPHORE_SPECIFICATIONS_SEMAPHORE_DIRECTORY";
   internal const string UnitsOfTheSemaphoreVariable = "COMPZE_PROCESS_SEMAPHORE_SPECIFICATIONS_UNITS_OF_THE_SEMAPHORE";
   internal const string UnitsToTakeVariable = "COMPZE_PROCESS_SEMAPHORE_SPECIFICATIONS_UNITS_TO_TAKE";

   ///<summary>The file that <see cref="take_the_units_and_hold_them_until_killed"/> creates once it holds the units.</summary>
   internal const string HoldingFileVariable = "COMPZE_PROCESS_SEMAPHORE_SPECIFICATIONS_HOLDING_FILE";

   ///<summary>What the child process holds units for, which those who wait meanwhile are told.</summary>
   internal const string HoldingPurpose = "a specification that has another process hold units until it is killed";

   ///<summary>What the child process waits for units for, which those who wait after it are told.</summary>
   internal const string WaitingPurpose = "a specification that has another process wait for units until it is killed";

   ///<summary>Holds the units until the process is killed, which it would be long before the end of its wait.</summary>
   [XF(Explicit = true)] public async Task take_the_units_and_hold_them_until_killed()
   {
      using var hold = await TheSemaphore().TakeAsync(UnitsToTake(), HoldingPurpose, WaitTimeout.Seconds(30), TestContext.Current.CancellationToken);
      await File.WriteAllTextAsync(Variable(HoldingFileVariable), "", TestContext.Current.CancellationToken);
      await Task.Delay(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
   }

   ///<summary>Waits for the units until the process is killed, which it would be long before the end of its wait.</summary>
   [XF(Explicit = true)] public async Task wait_for_the_units_until_killed() =>
      (await TheSemaphore().TryTakeAsync(UnitsToTake(), WaitingPurpose, WaitTimeout.Minutes(5), TestContext.Current.CancellationToken))?.Dispose();

   static IProcessSemaphore TheSemaphore() => IProcessSemaphore.New(Variable(SemaphoreNameVariable), int.Parse(Variable(UnitsOfTheSemaphoreVariable), CultureInfo.InvariantCulture), new DirectoryInfo(Variable(SemaphoreDirectoryVariable)));

   static int UnitsToTake() => int.Parse(Variable(UnitsToTakeVariable), CultureInfo.InvariantCulture);

   static string Variable(string name) => Environment.GetEnvironmentVariable(name)
                                       ?? throw new InvalidOperationException($"The environment variable {name} isn't set: only a specification of IProcessSemaphore runs this scenario, in a process of its own.");
}
