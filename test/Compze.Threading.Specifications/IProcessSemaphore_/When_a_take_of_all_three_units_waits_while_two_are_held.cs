using System.Text.RegularExpressions;
using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

///<summary>Takes are served in the order they came: a take of one unit, begun after a take of three that waits, waits behind it, though one unit is<br/>
/// free.</summary>
public class When_a_take_of_all_three_units_waits_while_two_are_held : ProcessSemaphoreSpecification
{
   const string EarlierTakesPurpose = "the earlier take, of all three units";

   readonly IProcessSemaphoreHold _twoUnits;
   readonly Task<IProcessSemaphoreHold?> _earlierTake;

   public When_a_take_of_all_three_units_waits_while_two_are_held()
   {
      _twoUnits = Holding(AnotherInstanceOfTheSemaphore().Take(2, Purpose, WaitTimeout.Zero))!;
      _earlierTake = Waiting(3, EarlierTakesPurpose);
   }

   [XF] public void a_later_TryTake_of_the_one_free_unit_returns_null_when_it_gives_up() =>
      Holding(AnotherInstanceOfTheSemaphore().TryTake(1, Purpose, WaitTimeout.Milliseconds(200))).Must().BeNull();

   [XF] public void a_later_Take_that_gives_up_names_the_take_waiting_ahead_of_it() =>
      Invoking(() => Holding(AnotherInstanceOfTheSemaphore().Take(1, Purpose, WaitTimeout.Milliseconds(100)))).Must().Throw<TimeoutException>()
                                                                                                              .Which.Message.Must().Contain($"3 units for {EarlierTakesPurpose}");

   [XF] public void DescribeHoldersAndWaiters_says_who_waits() =>
      Semaphore.DescribeHoldersAndWaiters().Must().Contain($"Waiting, in the order they came: 3 units for {EarlierTakesPurpose}");

   public class and_the_two_held_units_are_released : When_a_take_of_all_three_units_waits_while_two_are_held
   {
      public and_the_two_held_units_are_released() => _twoUnits.Dispose();

      [XF] public async Task the_waiting_take_returns_a_hold_of_all_three_units() => ((await _earlierTake.WaitAsync(Patience))?.Units).Must().Be(3);
   }

   public class and_a_take_of_one_unit_waits_behind_it : When_a_take_of_all_three_units_waits_while_two_are_held
   {
      const string LaterTakesPurpose = "the later take, of one unit";

      readonly Task<IProcessSemaphoreHold?> _laterTake;

      public and_a_take_of_one_unit_waits_behind_it() => _laterTake = Waiting(1, LaterTakesPurpose);

      [XF] public void DescribeHoldersAndWaiters_says_who_waits_in_the_order_they_came() =>
         Semaphore.DescribeHoldersAndWaiters().Must().Satisfy(it => Regex.IsMatch(it, $"Waiting, in the order they came: 3 units for {Regex.Escape(EarlierTakesPurpose)}, by .*; 1 unit for {Regex.Escape(LaterTakesPurpose)}, by "));

      public class and_the_two_held_units_are_released_while_both_wait : and_a_take_of_one_unit_waits_behind_it
      {
         IProcessSemaphoreHold? _earlierTakesHold;

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            _twoUnits.Dispose();
            _earlierTakesHold = await _earlierTake.WaitAsync(Patience);
         }

         [XF] public void the_earlier_take_returns_a_hold_of_all_three_units() => (_earlierTakesHold?.Units).Must().Be(3);

         [XF] public void the_later_take_still_waits() =>
            Semaphore.DescribeHoldersAndWaiters().Must().Contain($"Waiting, in the order they came: 1 unit for {LaterTakesPurpose}");

         public class and_the_earlier_take_releases_its_units : and_the_two_held_units_are_released_while_both_wait
         {
            protected override async Task InitializeAsyncInternal()
            {
               await base.InitializeAsyncInternal();
               _earlierTakesHold!.Dispose();
            }

            [XF] public async Task the_later_take_returns_a_hold_of_one_unit() => ((await _laterTake.WaitAsync(Patience))?.Units).Must().Be(1);
         }
      }
   }
}
