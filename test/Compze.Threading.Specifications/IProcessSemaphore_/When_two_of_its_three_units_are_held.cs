using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

public class When_two_of_its_three_units_are_held : ProcessSemaphoreSpecification
{
   const string HoldersPurpose = "a specification that holds two units";

   readonly IProcessSemaphoreHold _twoUnits;

   public When_two_of_its_three_units_are_held() => _twoUnits = Holding(AnotherInstanceOfTheSemaphore().Take(2, HoldersPurpose, WaitTimeout.Zero))!;

   [XF] public void TryTake_of_the_third_unit_with_a_WaitTimeout_of_zero_returns_a_hold_of_it() =>
      (Holding(Semaphore.TryTake(1, Purpose, WaitTimeout.Zero))?.Units).Must().Be(1);

   [XF] public void TryTake_of_two_units_with_a_WaitTimeout_of_zero_returns_null() =>
      Holding(Semaphore.TryTake(2, Purpose, WaitTimeout.Zero)).Must().BeNull();

   [XF] public void a_Take_of_two_units_that_gives_up_says_what_the_held_units_are_held_for() =>
      Invoking(() => Holding(Semaphore.Take(2, Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                                       .Which.Message.Must().Contain($"2 units for {HoldersPurpose}");

   [XF] public void a_Take_of_two_units_that_gives_up_names_the_process_holding_them() =>
      Invoking(() => Holding(Semaphore.Take(2, Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                                       .Which.Message.Must().Contain($"process {Environment.ProcessId} (");

   [XF] public void DescribeHoldersAndWaiters_says_how_many_units_are_held_and_what_for() =>
      Semaphore.DescribeHoldersAndWaiters().Must().Contain($"Held: 2 units for {HoldersPurpose}");

   public class and_the_hold_is_disposed : When_two_of_its_three_units_are_held
   {
      public and_the_hold_is_disposed() => _twoUnits.Dispose();

      [XF] public void all_three_units_can_be_taken() => Holding(Semaphore.TryTake(3, Purpose, WaitTimeout.Zero)).Must().NotBeNull();

      [XF] public void disposing_it_again_releases_nothing_another_holder_took()
      {
         Holding(Semaphore.Take(3, Purpose, WaitTimeout.Zero));
         _twoUnits.Dispose();
         Holding(AnotherInstanceOfTheSemaphore().TryTake(1, Purpose, WaitTimeout.Zero)).Must().BeNull();
      }
   }

   public class and_the_hold_is_disposed_on_another_thread : When_two_of_its_three_units_are_held
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await Task.Run(_twoUnits.Dispose).WaitAsync(Patience);
      }

      [XF] public void all_three_units_can_be_taken() => Holding(Semaphore.TryTake(3, Purpose, WaitTimeout.Zero)).Must().NotBeNull();
   }
}
