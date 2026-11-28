using Compze.Must;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IProcessLock_;

public class When_no_one_holds_the_lock : ProcessLockSpecification
{
   [XF] public void TryTake_with_a_WaitTimeout_of_zero_returns_a_hold() => Holding(Lock.TryTake(Purpose, WaitTimeout.Zero)).Must().NotBeNull();
   [XF] public void Take_with_a_WaitTimeout_of_zero_returns_a_hold() => Holding(Lock.Take(Purpose, WaitTimeout.Zero)).Must().NotBeNull();
   [XF] public async Task TryTakeAsync_with_a_WaitTimeout_of_zero_returns_a_hold() => Holding(await Lock.TryTakeAsync(Purpose, WaitTimeout.Zero)).Must().NotBeNull();
   [XF] public async Task TakeAsync_with_a_WaitTimeout_of_zero_returns_a_hold() => Holding(await Lock.TakeAsync(Purpose, WaitTimeout.Zero)).Must().NotBeNull();
   [XF] public void TryTake_with_an_infinite_WaitTimeout_returns_a_hold() => Holding(Lock.TryTake(Purpose, WaitTimeout.Infinite)).Must().NotBeNull();

   [XF] public void LastHoldersDescription_of_a_lock_no_holder_has_ever_taken_is_null() => Lock.LastHoldersDescription().Must().BeNull();

   [XF] public void TryTake_for_a_purpose_of_only_whitespace_throws_an_ArgumentException() =>
      Invoking(() => Holding(Lock.TryTake(" ", WaitTimeout.Zero))).Must().Throw<ArgumentException>();

   [XF] public async Task TryTakeAsync_for_a_purpose_of_only_whitespace_throws_an_ArgumentException() =>
      await InvokingAsync(async () => Holding(await Lock.TryTakeAsync(" ", WaitTimeout.Zero))).Must().ThrowAsync<ArgumentException>();
}
