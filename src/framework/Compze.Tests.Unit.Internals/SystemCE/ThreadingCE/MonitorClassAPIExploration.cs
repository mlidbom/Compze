﻿using System;
using System.Threading;
using Compze.SystemCE;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Testing.Threading;
using FluentAssertions;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE;

public class MonitorClassApiExploration
{
   [XFact] public void Wait_returns_after_timeout_even_without_pulse()
   {
      var guarded = new object();

      Monitor.Enter(guarded);
      Monitor.Wait(guarded, 1.Milliseconds())
             .Should()
             .BeFalse();
   }

   [XFact] public void Wait_does_not_return_return_until_lock_is_available_to_reacquire_after_timeout()
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
                                 .TryAwaitPassededThroughCountEqualTo(1, timeout: 200.Milliseconds())
                                 .Should().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Should().Be(false);
   }

   [XFact] public void Wait_does_not_hang_on_long_timeout_values()
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
                                 .TryAwaitPassededThroughCountEqualTo(1, timeout: 200.Milliseconds())
                                 .Should().Be(false);

      threadTwoHasAcquiredLockAndWishesToReleaseItGate.AwaitLetOneThreadPassThrough();

      threadOneWaitsOnLockSection.ExitGate.AwaitPassedThroughCountEqualTo(1);

      waitSucceeded.Should().Be(true);
   }
}