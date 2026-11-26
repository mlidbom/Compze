using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.IProcessSemaphore_.Infrastructure;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

public class When_another_process_holds_two_of_its_three_units : ProcessSemaphoreSpecification
{
   AnotherProcessUsingTheSemaphore? _otherProcess;

   AnotherProcessUsingTheSemaphore OtherProcess => _otherProcess!;

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      _otherProcess = await AnotherProcessUsingTheSemaphore.HoldingAsync(2, Semaphore, SemaphoreName, SemaphoreDirectory, Patience);
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_otherProcess != null) await _otherProcess.DisposeAsync(); //Null when it failed to start, which the failing specification reports.
      await base.DisposeAsyncInternal();
   }

   [XF] public void TryTake_of_two_units_with_a_WaitTimeout_of_zero_returns_null() => Holding(Semaphore.TryTake(2, Purpose, WaitTimeout.Zero)).Must().BeNull();

   [XF] public void TryTake_of_the_third_unit_with_a_WaitTimeout_of_zero_returns_a_hold_of_it() =>
      (Holding(Semaphore.TryTake(1, Purpose, WaitTimeout.Zero))?.Units).Must().Be(1);

   [XF] public void a_Take_that_gives_up_says_what_the_other_process_holds_the_units_for() =>
      Invoking(() => Holding(Semaphore.Take(2, Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                                       .Which.Message.Must().Contain($"2 units for {ProcessSemaphoreScenariosForAChildProcess.HoldingPurpose}");

   [XF] public void a_Take_that_gives_up_names_the_other_process() =>
      Invoking(() => Holding(Semaphore.Take(2, Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                                       .Which.Message.Must().Contain($"process {OtherProcess.ProcessId} (");

   public class and_the_other_process_is_killed : When_another_process_holds_two_of_its_three_units
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await OtherProcess.KillAsync();
      }

      [XF] public void all_three_units_can_be_taken() => (Holding(Semaphore.TryTake(3, Purpose, WaitTimeout.Seconds(10)))?.Units).Must().Be(3);
   }

   public class and_the_other_process_is_killed_while_a_take_of_all_three_units_waits : When_another_process_holds_two_of_its_three_units
   {
      Task<IProcessSemaphoreHold?>? _take;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _take = Waiting(3, Purpose);
         await OtherProcess.KillAsync();
      }

      [XF] public async Task the_take_returns_a_hold_of_all_three_units() => ((await _take!.WaitAsync(Patience))?.Units).Must().Be(3);
   }
}
