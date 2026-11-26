using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.IProcessLock_.Infrastructure;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessLock_;

public class When_another_process_holds_the_lock : ProcessLockSpecification
{
   AnotherProcessHoldingTheLock? _otherProcess;

   AnotherProcessHoldingTheLock OtherProcess => _otherProcess!;

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      _otherProcess = await AnotherProcessHoldingTheLock.StartAsync(LockName, LockDirectory, Patience);
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_otherProcess != null) await _otherProcess.DisposeAsync(); //Null when it failed to start, which the failing specification reports.
      await base.DisposeAsyncInternal();
   }

   [XF] public void TryTake_with_a_WaitTimeout_of_zero_returns_null() => Holding(Lock.TryTake(Purpose, WaitTimeout.Zero)).Must().BeNull();

   [XF] public void a_Take_that_gives_up_says_what_the_other_process_holds_it_for() =>
      Invoking(() => Holding(Lock.Take(Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                               .Which.Message.Must().Contain(ProcessLockScenariosForAChildProcess.ChildProcessPurpose);

   [XF] public void a_Take_that_gives_up_names_the_other_process() =>
      Invoking(() => Holding(Lock.Take(Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                               .Which.Message.Must().Contain($"process {OtherProcess.ProcessId} (");

   public class and_the_other_process_is_killed : When_another_process_holds_the_lock
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await OtherProcess.KillAsync();
      }

      [XF] public void the_lock_can_be_taken() => Holding(Lock.TryTake(Purpose, WaitTimeout.Seconds(10))).Must().NotBeNull();
   }

   public class and_the_other_process_is_killed_while_TryTakeAsync_waits : When_another_process_holds_the_lock
   {
      Task<IProcessLockHold?>? _wait;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _wait = Lock.TryTakeAsync(Purpose, WaitTimeout.Seconds(30));
         await OtherProcess.KillAsync();
      }

      [XF] public async Task the_wait_returns_a_hold() => Holding(await _wait!.WaitAsync(Patience)).Must().NotBeNull();
   }
}
