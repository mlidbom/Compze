using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

///<summary>Another instance of the semaphore, of the same name in the same directory, made with five units where the first was made with three.</summary>
public class When_it_is_made_with_another_number_of_units : ProcessSemaphoreSpecification
{
   readonly IProcessSemaphore _withFiveUnits;

   public When_it_is_made_with_another_number_of_units() => _withFiveUnits = AnotherInstanceOfTheSemaphore(units: 5);

   [XF] public void it_has_the_units_it_was_made_with() => _withFiveUnits.Units.Must().Be(5);

   public class while_one_of_the_three_units_is_held : When_it_is_made_with_another_number_of_units
   {
      public while_one_of_the_three_units_is_held() => Holding(Semaphore.Take(1, Purpose, WaitTimeout.Zero));

      [XF] public void a_take_of_its_units_throws_an_InvalidOperationException_saying_that_the_semaphore_has_three() =>
         Invoking(() => Holding(_withFiveUnits.TryTake(1, Purpose, WaitTimeout.Zero))).Must().Throw<InvalidOperationException>()
                                                                                     .Which.Message.Must().Contain("it has 3, which are held or waited for");
   }

   public class once_the_three_units_have_been_taken_and_released : When_it_is_made_with_another_number_of_units
   {
      public once_the_three_units_have_been_taken_and_released() => Semaphore.Take(3, Purpose, WaitTimeout.Zero).Dispose();

      [XF] public void all_five_of_its_units_can_be_taken() => (Holding(_withFiveUnits.TryTake(5, Purpose, WaitTimeout.Zero))?.Units).Must().Be(5);

      public class and_its_five_units_are_held : once_the_three_units_have_been_taken_and_released
      {
         public and_its_five_units_are_held() => Holding(_withFiveUnits.Take(5, Purpose, WaitTimeout.Zero));

         [XF] public void a_take_of_the_instance_made_with_three_units_throws_an_InvalidOperationException_saying_that_the_semaphore_has_five() =>
            Invoking(() => Holding(Semaphore.TryTake(1, Purpose, WaitTimeout.Zero))).Must().Throw<InvalidOperationException>()
                                                                                    .Which.Message.Must().Contain("it has 5, which are held or waited for");
      }
   }
}
