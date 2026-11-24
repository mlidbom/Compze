using Compze.Internals.SystemCE;
using Compze.Threading.Testing;
using Compze.Must;

using Compze.xUnitBDD;
using Xunit;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.Threading.Tests;

[Collection(nameof(NonParallelCollection))]
public class MonitorClassApiExploration
{
   [XF] public void Wait_returns_after_timeout_even_without_pulse()
   {
      var guarded = new object();

      Monitor.Enter(guarded);
      Monitor.Wait(guarded, 1.Milliseconds())
             .Must()
             .BeFalse();
   }

   [XF] public void Wait_does_not_return_return_until_lock_is_available_to_reacquire_after_timeout()
   {
      var guarded = new object();

      var threadOneWaitsOnLockSection = IGatedCodeSection.NewOpen(WaitTimeout.Seconds(30), "threadOneWaitsOnLockSection");
      var threadTwoHasAcquiredLockAndWishesToReleaseItGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "threadTwoHasAcquiredLockAndWishesToReleaseItGate");

      //Thread two must take the lock while thread one waits, and the wait times out after waitTimeout: long enough for thread
      //two's task to start and take the lock even when every test project runs at once. At 100 milliseconds it sometimes didn't.
      var waitTimeout = 1.Seconds();

      var waitSucceeded = false;
      using var taskRunner = TestingTaskRunner.WithTimeout(30.Seconds());
      taskRunner.Start(() =>
      {
         Monitor.Enter(guarded);
         try
         {
            threadOneWaitsOnLockSection.Execute(() => waitSucceeded = Monitor.Wait(guarded, waitTimeout));
         }
         finally
         {
            Monitor.Exit(guarded); //Should thread two have come too late, so thread one took the lock back, the test then fails its assertions instead of leaving thread two blocked forever.
         }
      });

      threadOneWaitsOnLockSection.EntranceGate.AwaitPassedThroughCountEqualTo(1);

      taskRunner.Start(() =>
      {
         Monitor.Enter(guarded);
         threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitPassThrough();
         Monitor.Exit(guarded);
      });

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitQueueLengthEqualTo(1);

      threadOneWaitsOnLockSection.ExitGate
                                 .TryAwaitPassedThroughCountEqualTo(1, new WaitTimeout(waitTimeout + 200.Milliseconds()))
                                 .Must().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Must().Be(false);
   }

   [XF] public void Wait_does_not_hang_on_long_timeout_values()
   {
      var guarded = new object();

      var threadOneWaitsOnLockSection = IGatedCodeSection.NewOpen(WaitTimeout.Seconds(30), "threadOneWaitsOnLockSection");
      var threadTwoHasAcquiredLockAndWishesToReleaseItGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "threadTwoHasAcquiredLockAndWishesToReleaseItGate");

      var waitTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

      var waitSucceeded = false;
      using var taskRunner = TestingTaskRunner.WithTimeout(30.Seconds());
      taskRunner.Start(() =>
      {
         Monitor.Enter(guarded);
         threadOneWaitsOnLockSection.Execute(() => waitSucceeded = Monitor.Wait(guarded, waitTimeout));
      });

      threadOneWaitsOnLockSection.EntranceGate.AwaitPassedThroughCountEqualTo(1);

      taskRunner.Start(() =>
      {
         Monitor.Enter(guarded);
         threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitPassThrough();
         Monitor.PulseAll(guarded);
         Monitor.Exit(guarded);
      });

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitQueueLengthEqualTo(1);

      threadOneWaitsOnLockSection.ExitGate
                                 .TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(200))
                                 .Must().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Must().Be(true);
   }
}
