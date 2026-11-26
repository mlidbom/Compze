using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessLock_.Infrastructure;

///<summary>What the specifications of <see cref="IProcessLock"/> have another process do with the lock, through <see cref="ExplicitTestInAChildProcess"/>.<br/>
/// Each is explicit, so that no ordinary test run runs it, and takes the lock its environment variables name.</summary>
public class ProcessLockScenariosForAChildProcess
{
   internal const string LockNameVariable = "COMPZE_PROCESS_LOCK_SPECIFICATIONS_LOCK_NAME";
   internal const string LockDirectoryVariable = "COMPZE_PROCESS_LOCK_SPECIFICATIONS_LOCK_DIRECTORY";

   ///<summary>The file that <see cref="take_the_lock_and_hold_it_until_killed"/> creates once it holds the lock.</summary>
   internal const string HoldingFileVariable = "COMPZE_PROCESS_LOCK_SPECIFICATIONS_HOLDING_FILE";

   ///<summary>What the child process holds the lock for, which those who wait for it are told.</summary>
   internal const string ChildProcessPurpose = "a specification that has another process hold it until it is killed";

   ///<summary>Holds the lock until the process is killed, which it would be long before the end of its wait.</summary>
   [XF(Explicit = true)] public async Task take_the_lock_and_hold_it_until_killed()
   {
      using var hold = await TheLock().TakeAsync(ChildProcessPurpose, WaitTimeout.Seconds(30), TestContext.Current.CancellationToken);
      await File.WriteAllTextAsync(Variable(HoldingFileVariable), "", TestContext.Current.CancellationToken);
      await Task.Delay(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
   }

   [XF(Explicit = true)] public void taking_the_lock_throws_a_StateAssertionFailedException() =>
      Invoking(() => TheLock().TryTake("a specification that a lock file that keeps no one out is refused", WaitTimeout.Zero)).Must().Throw<StateAssertionFailedException>();

   static IProcessLock TheLock() => IProcessLock.New(Variable(LockNameVariable), new DirectoryInfo(Variable(LockDirectoryVariable)));

   static string Variable(string name) => Environment.GetEnvironmentVariable(name)
                                       ?? throw new InvalidOperationException($"The environment variable {name} isn't set: only a specification of IProcessLock runs this scenario, in a process of its own.");
}
