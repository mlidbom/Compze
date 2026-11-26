using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

///<summary>Two of the three units are held, a take of all three waits first, and a take of the one free unit waits behind it, until the first gives<br/>
/// up: then the second takes the free unit.</summary>
public abstract class When_the_take_waiting_first_gives_up : ProcessSemaphoreSpecification
{
   const string FirstTakesPurpose = "the first take, of all three units";
   const string SecondTakesPurpose = "the second take, of one unit";

   protected When_the_take_waiting_first_gives_up() => Holding(AnotherInstanceOfTheSemaphore().Take(2, Purpose, WaitTimeout.Zero));

   public class as_its_WaitTimeout_passes : When_the_take_waiting_first_gives_up
   {
      readonly Task<IProcessSemaphoreHold?> _firstTake;
      readonly Task<IProcessSemaphoreHold?> _secondTake;

      public as_its_WaitTimeout_passes()
      {
         _firstTake = Waiting(3, FirstTakesPurpose, WaitTimeout.Milliseconds(200));
         _secondTake = Waiting(1, SecondTakesPurpose);
      }

      [XF] public async Task the_first_take_returns_null() => (await _firstTake.WaitAsync(Patience)).Must().BeNull();

      [XF] public async Task the_second_take_returns_a_hold_of_the_free_unit() => ((await _secondTake.WaitAsync(Patience))?.Units).Must().Be(1);
   }

   public class as_it_is_cancelled : When_the_take_waiting_first_gives_up
   {
      readonly Task<IProcessSemaphoreHold?> _firstTake;
      readonly Task<IProcessSemaphoreHold?> _secondTake;

      public as_it_is_cancelled()
      {
         _firstTake = Waiting(3, FirstTakesPurpose);
         _secondTake = Waiting(1, SecondTakesPurpose);
         GivesUp(_firstTake);
      }

      [XF] public async Task the_first_take_throws_an_OperationCanceledException() =>
         await InvokingAsync(async () => await _firstTake.WaitAsync(Patience)).Must().ThrowAsync<OperationCanceledException>();

      [XF] public async Task the_second_take_returns_a_hold_of_the_free_unit() => ((await _secondTake.WaitAsync(Patience))?.Units).Must().Be(1);
   }
}
