using Compze.Must;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Specifications.IProcessLock_.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Threading.Specifications.IProcessLock_;

///<summary>On Unix .NET locks files with <c>flock</c>, unless its file locking is turned off: then a lock file keeps no second holder out, and taking<br/>
/// the lock must fail rather than let one in. Windows keeps others out by sharing modes, which can't be turned off.</summary>
public class When_dotnets_file_locking_is_turned_off : ProcessLockSpecification
{
   const string OnlyUnixLocksFilesWithFlock = "Only on Unix does .NET lock files with flock, which can be turned off.";

   public static bool OnUnix => !OperatingSystem.IsWindows();

   [XF(Skip = OnlyUnixLocksFilesWithFlock, SkipType = typeof(When_dotnets_file_locking_is_turned_off), SkipUnless = nameof(OnUnix))]
   public async Task taking_the_lock_throws_a_StateAssertionFailedException() =>
      (await ExplicitTestInAChildProcess.RunAsync(typeof(ProcessLockScenariosForAChildProcess),
                                                  nameof(ProcessLockScenariosForAChildProcess.taking_the_lock_throws_a_StateAssertionFailedException),
                                                  Patience,
                                                  new Dictionary<string, string>
                                                  {
                                                     ["DOTNET_SYSTEM_IO_DISABLEFILELOCKING"] = "true",
                                                     [ProcessLockScenariosForAChildProcess.LockNameVariable] = LockName,
                                                     [ProcessLockScenariosForAChildProcess.LockDirectoryVariable] = LockDirectory.FullName
                                                  }))
     .Must().Contain(ExplicitTestInAChildProcess.SummaryOfOnePassingTest);
}
