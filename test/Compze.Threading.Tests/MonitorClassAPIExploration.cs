using Compze.Utilities.SystemCE;
using Compze.Threading.Testing;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
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

      var threadOneWaitsOnLockSection = GatedCodeSection.Closed(WaitTimeout.Seconds(30), "threadOneWaitsOnLockSection").Open();
      var threadTwoHasAcquiredLockAndWishesToReleaseItGate = ThreadGate.Closed(WaitTimeout.Seconds(30), "threadTwoHasAcquiredLockAndWishesToReleaseItGate");

      var waitTimeout = 100.Milliseconds();

      var waitSucceeded = false;
      using var taskRunner = TestingTaskRunner.WithTimeout(30.Seconds());
      taskRunner.Run(() =>
      {
         Monitor.Enter(guarded);
         threadOneWaitsOnLockSection.Execute(() => waitSucceeded = Monitor.Wait(guarded, waitTimeout));
      });

      threadOneWaitsOnLockSection.EntranceGate.AwaitPassedThroughCountEqualTo(1);

      taskRunner.Run(() =>
      {
         Monitor.Enter(guarded);
         threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitPassThrough();
         Monitor.Exit(guarded);
      });

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitQueueLengthEqualTo(1);

      threadOneWaitsOnLockSection.ExitGate
                                 .TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(200))
                                 .Must().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Must().Be(false);
   }

   [XF] public void Wait_does_not_hang_on_long_timeout_values()
   {
      var guarded = new object();

      var threadOneWaitsOnLockSection = GatedCodeSection.Closed(WaitTimeout.Seconds(30), "threadOneWaitsOnLockSection").Open();
      var threadTwoHasAcquiredLockAndWishesToReleaseItGate = ThreadGate.Closed(WaitTimeout.Seconds(30), "threadTwoHasAcquiredLockAndWishesToReleaseItGate");

      var waitTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

      var waitSucceeded = false;
      using var taskRunner = TestingTaskRunner.WithTimeout(30.Seconds());
      taskRunner.Run(() =>
      {
         Monitor.Enter(guarded);
         threadOneWaitsOnLockSection.Execute(() => waitSucceeded = Monitor.Wait(guarded, waitTimeout));
      });

      threadOneWaitsOnLockSection.EntranceGate.AwaitPassedThroughCountEqualTo(1);

      taskRunner.Run(() =>
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
