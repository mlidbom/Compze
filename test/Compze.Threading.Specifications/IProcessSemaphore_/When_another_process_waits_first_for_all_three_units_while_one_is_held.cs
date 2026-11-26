using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.IProcessSemaphore_.Infrastructure;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

///<summary>The other process waits first in the queue, so it takes the two free units as it waits for the third, which this process holds.</summary>
public class When_another_process_waits_first_for_all_three_units_while_one_is_held : ProcessSemaphoreSpecification
{
   AnotherProcessUsingTheSemaphore? _otherProcess;

   AnotherProcessUsingTheSemaphore OtherProcess => _otherProcess!;

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      Holding(await AnotherInstanceOfTheSemaphore().TakeAsync(1, Purpose, WaitTimeout.Zero));
      _otherProcess = await AnotherProcessUsingTheSemaphore.WaitingAsync(3, Semaphore, SemaphoreName, SemaphoreDirectory, Patience);
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_otherProcess != null) await _otherProcess.DisposeAsync(); //Null when it failed to start, which the failing specification reports.
      await base.DisposeAsyncInternal();
   }

   [XF] public void DescribeHoldersAndWaiters_names_the_other_process_among_those_who_wait() =>
      Semaphore.DescribeHoldersAndWaiters().Must().Contain($"3 units for {ProcessSemaphoreScenariosForAChildProcess.WaitingPurpose}, by process {OtherProcess.ProcessId} (");

   public class and_the_other_process_is_killed_while_a_take_of_two_units_waits_behind_it : When_another_process_waits_first_for_all_three_units_while_one_is_held
   {
      Task<IProcessSemaphoreHold?>? _take;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _take = Waiting(2, Purpose);
         await OtherProcess.KillAsync();
      }

      [XF] public async Task the_take_returns_a_hold_of_the_two_units_the_other_process_had_taken() => ((await _take!.WaitAsync(Patience))?.Units).Must().Be(2);

      [XF] public async Task DescribeHoldersAndWaiters_names_no_one_waiting()
      {
         await _take!.WaitAsync(Patience);
         Semaphore.DescribeHoldersAndWaiters().Must().Contain("None waits.");
      }
   }
}
