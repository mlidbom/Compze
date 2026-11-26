using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IProcessLock_;

public class When_a_hold_is_disposed : ProcessLockSpecification
{
   readonly IProcessLockHold _hold;

   public When_a_hold_is_disposed()
   {
      _hold = Lock.Take(Purpose, WaitTimeout.Zero);
      _hold.Dispose();
   }

   [XF] public void the_lock_can_be_taken_again() => Holding(AnotherInstanceOfTheLock().TryTake(Purpose, WaitTimeout.Zero)).Must().NotBeNull();

   public class and_disposed_again_after_another_holder_has_taken_the_lock : When_a_hold_is_disposed
   {
      public and_disposed_again_after_another_holder_has_taken_the_lock()
      {
         Holding(AnotherInstanceOfTheLock().Take(Purpose, WaitTimeout.Zero));
         _hold.Dispose();
      }

      [XF] public void the_other_holder_still_holds_the_lock() => Holding(Lock.TryTake(Purpose, WaitTimeout.Zero)).Must().BeNull();
   }
}
