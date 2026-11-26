using Compze.Must;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessSemaphore_;

public class When_no_one_holds_its_units : ProcessSemaphoreSpecification
{
   [XF] public void TryTake_of_all_its_units_with_a_WaitTimeout_of_zero_returns_a_hold_of_all_of_them() =>
      (Holding(Semaphore.TryTake(UnitsOfTheSemaphore, Purpose, WaitTimeout.Zero))?.Units).Must().Be(UnitsOfTheSemaphore);

   [XF] public void Take_of_two_units_returns_a_hold_of_two_units() =>
      (Holding(Semaphore.Take(2, Purpose, WaitTimeout.Zero))?.Units).Must().Be(2);

   [XF] public async Task TakeAsync_of_one_unit_returns_a_hold_of_one_unit() =>
      (Holding(await Semaphore.TakeAsync(1, Purpose, WaitTimeout.Zero))?.Units).Must().Be(1);

   [XF] public void a_take_of_more_units_than_it_has_throws_an_ArgumentException() =>
      Invoking(() => Holding(Semaphore.TryTake(UnitsOfTheSemaphore + 1, Purpose, WaitTimeout.Zero))).Must().Throw<ArgumentException>();

   [XF] public void a_take_of_no_units_throws_an_ArgumentException() =>
      Invoking(() => Holding(Semaphore.TryTake(0, Purpose, WaitTimeout.Zero))).Must().Throw<ArgumentException>();

   [XF] public void a_take_for_a_purpose_of_only_whitespace_throws_an_ArgumentException() =>
      Invoking(() => Holding(Semaphore.TryTake(1, " ", WaitTimeout.Zero))).Must().Throw<ArgumentException>();

   [XF] public void a_take_with_a_cancelled_CancellationToken_throws_an_OperationCanceledException() =>
      Invoking(() => Holding(Semaphore.TryTake(1, Purpose, WaitTimeout.Zero, new CancellationToken(canceled: true)))).Must().Throw<OperationCanceledException>();

   [XF] public void DescribeHoldersAndWaiters_says_that_none_is_held_and_none_waits() =>
      Semaphore.DescribeHoldersAndWaiters().Must().Contain("None is held. None waits.");
}
