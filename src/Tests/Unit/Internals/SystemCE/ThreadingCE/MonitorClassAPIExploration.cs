using System;
using System.Threading;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE;

public class MonitorClassApiExploration
{
   [XF] public void Wait_returns_after_timeout_even_without_pulse_but_when_it_does_it_returns_false()
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

      var threadOneWaitsOnLockSection = GatedCodeSection.WithTimeout(5.Seconds()).Open();
      var threadTwoHasAcquiredLockAndWishesToReleaseItGate = ThreadGate.CreateClosedWithTimeout(5.Seconds());

      var waitTimeout = 100.Milliseconds();

      var waitSucceeded = false;
      using var taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
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
                                 .TryAwaitPassedThroughCountEqualTo(1, timeout: 200.Milliseconds())
                                 .Must().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Must().Be(false);
   }

   [XF] public void Wait_does_not_hang_on_long_timeout_values()
   {
      var guarded = new object();

      var threadOneWaitsOnLockSection = GatedCodeSection.WithTimeout(5.Seconds()).Open();
      var threadTwoHasAcquiredLockAndWishesToReleaseItGate = ThreadGate.CreateClosedWithTimeout(5.Seconds());

      var waitTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

      var waitSucceeded = false;
      using var taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
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
                                 .TryAwaitPassedThroughCountEqualTo(1, timeout: 200.Milliseconds())
                                 .Must().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Must().Be(true);
   }
}
