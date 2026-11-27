using Compze.Contracts.Exceptions;
using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class ThrowOnReentrancyAsyncLock_specification : UniversalTestBase
{
   readonly IThrowOnReentrancyAsyncLock _lock = IThrowOnReentrancyAsyncLock.WithDefaultTimeout();

   public class Taken_by_one_flow : ThrowOnReentrancyAsyncLock_specification
   {
      [XF] public void Locked_returns_what_its_action_returns() => _lock.Locked(() => 42).Must().Be(42);

      [XF] public async Task LockedAsync_returns_what_its_action_returns() =>
         (await _lock.LockedAsync(async () =>
         {
            await Task.Yield();
            return 42;
         })).Must().Be(42);

      [XF] public void it_is_held_on_the_flow_inside_Locked() => _lock.Locked(() => _lock.IsHeldOnThisFlow).Must().BeTrue();

      [XF] public async Task it_is_held_on_the_flow_inside_LockedAsync_across_an_await() =>
         (await _lock.LockedAsync(async () =>
         {
            await Task.Yield();
            return _lock.IsHeldOnThisFlow;
         })).Must().BeTrue();

      [XF] public void it_is_not_held_on_a_flow_that_has_not_taken_it() => _lock.IsHeldOnThisFlow.Must().BeFalse();

      [XF] public void it_is_not_held_on_the_flow_once_Locked_has_returned()
      {
         _lock.Locked(() => {});
         _lock.IsHeldOnThisFlow.Must().BeFalse();
      }
   }

   public class Taken_again_from_inside_its_hold : ThrowOnReentrancyAsyncLock_specification
   {
      [XF] public void Locked_inside_Locked_throws_StateAssertionFailedException() =>
         Invoking(() => _lock.Locked(() => _lock.Locked(() => {}))).Must().Throw<StateAssertionFailedException>();

      [XF] public async Task LockedAsync_inside_LockedAsync_throws_StateAssertionFailedException() =>
         await InvokingAsync(async () => await _lock.LockedAsync(async () => await _lock.LockedAsync(async () => await Task.Yield())))
              .Must().ThrowAsync<StateAssertionFailedException>();

      [XF] public async Task Locked_inside_LockedAsync_throws_StateAssertionFailedException() =>
         await InvokingAsync(async () => await _lock.LockedAsync(async () =>
         {
            await Task.Yield();
            _lock.Locked(() => {});
         })).Must().ThrowAsync<StateAssertionFailedException>();

      [XF] public async Task LockedAsync_inside_Locked_returns_a_task_that_throws_StateAssertionFailedException()
      {
         var nestedUse = _lock.Locked(() => _lock.LockedAsync(async () => await Task.Yield()));
         await InvokingAsync(async () => await nestedUse).Must().ThrowAsync<StateAssertionFailedException>();
      }

      [XF] public async Task each_of_two_children_the_holder_starts_and_awaits_together_throws_StateAssertionFailedException()
      {
         Task[] children = [];
         await _lock.LockedAsync(async () =>
         {
            children = [Task.Run(() => _lock.Locked(() => {})), Task.Run(() => _lock.Locked(() => {}))];
            await Task.WhenAll(children).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
         });

         children.Select(it => it.Exception?.InnerException?.GetType()).Must().SequenceEqual([typeof(StateAssertionFailedException), typeof(StateAssertionFailedException)]);
      }

      [XF] public void the_holder_still_holds_the_lock_after_the_refusal() =>
         _lock.Locked(() =>
         {
            Invoking(() => _lock.Locked(() => {})).Must().Throw<StateAssertionFailedException>();
            return _lock.IsHeldOnThisFlow;
         }).Must().BeTrue();

      [XF] public async Task another_flow_takes_the_lock_once_the_holder_has_released_it_after_the_refusal()
      {
         _lock.Locked(() => Invoking(() => _lock.Locked(() => {})).Must().Throw<StateAssertionFailedException>());
         (await TaskCE.RunOnDedicatedThreadAsync(() => _lock.Locked(() => 42))).Must().Be(42);
      }
   }

   public class Taken_by_independent_flows : ThrowOnReentrancyAsyncLock_specification
   {
      readonly IThreadGate _theFirstHolderReleases = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "theFirstHolderReleases");
      readonly IThreadGate _theSecondTookTheLock = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "theSecondTookTheLock");

      [XF] public async Task with_LockedAsync_the_second_waits_until_the_first_has_released_it()
      {
         var first = _lock.LockedAsync(async () =>
         {
            await Task.Yield();
            await _theFirstHolderReleases.AwaitPassThroughAsync();
         });
         await _theFirstHolderReleases.AwaitQueueLengthEqualToAsync(1);

         var second = _lock.LockedAsync(_theSecondTookTheLock.AwaitPassThroughAsync);
         await TheSecondWaitsUntilTheFirstHasReleasedTheLock();
         await Task.WhenAll(first, second);
      }

      [XF] public async Task with_Locked_on_threads_of_their_own_the_second_waits_until_the_first_has_released_it()
      {
         var first = TaskCE.RunOnDedicatedThreadAsync(() => _lock.Locked(_theFirstHolderReleases.AwaitPassThrough));
         await _theFirstHolderReleases.AwaitQueueLengthEqualToAsync(1);

         var second = TaskCE.RunOnDedicatedThreadAsync(() => _lock.Locked(_theSecondTookTheLock.AwaitPassThrough));
         await TheSecondWaitsUntilTheFirstHasReleasedTheLock();
         await Task.WhenAll(first, second);
      }

      async Task TheSecondWaitsUntilTheFirstHasReleasedTheLock()
      {
         (await _theSecondTookTheLock.TryAwaitPassedThroughCountEqualToAsync(1, WaitTimeout.Milliseconds(20))).Must().BeFalse();
         _theFirstHolderReleases.Open();
         (await _theSecondTookTheLock.TryAwaitPassedThroughCountEqualToAsync(1, WaitTimeout.Seconds(5))).Must().BeTrue();
      }
   }

   ///<summary>Work started while the lock is held carries the holder's hold, and once the holder has released the lock the work is an
   /// independent flow, which takes its turn.</summary>
   public class Taken_by_work_started_inside_its_hold_once_the_hold_has_ended : ThrowOnReentrancyAsyncLock_specification
   {
      readonly IThreadGate _theLateWorkMayTakeTheLock = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "theLateWorkMayTakeTheLock");
      readonly IThreadGate _theLateWorkTookTheLock = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "theLateWorkTookTheLock");

      [XF] public async Task started_inside_Locked_it_waits_while_another_flow_holds_the_lock()
      {
         var lateWork = _lock.Locked(StartTheLateWork);
         await TheLateWorkWaitsWhileAnotherFlowHoldsTheLock(lateWork);
      }

      [XF] public async Task started_inside_LockedAsync_it_waits_while_another_flow_holds_the_lock()
      {
         var lateWork = await _lock.LockedAsync(async () =>
         {
            await Task.Yield();
            return StartTheLateWork();
         });
         await TheLateWorkWaitsWhileAnotherFlowHoldsTheLock(lateWork);
      }

      Task StartTheLateWork() => Task.Run(() =>
      {
         _theLateWorkMayTakeTheLock.AwaitPassThrough();
         _lock.Locked(_theLateWorkTookTheLock.AwaitPassThrough);
      });

      async Task TheLateWorkWaitsWhileAnotherFlowHoldsTheLock(Task lateWork)
      {
         var theOtherHolderReleases = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "theOtherHolderReleases");
         var otherHolder = TaskCE.RunOnDedicatedThreadAsync(() => _lock.Locked(theOtherHolderReleases.AwaitPassThrough));
         await theOtherHolderReleases.AwaitQueueLengthEqualToAsync(1);

         _theLateWorkMayTakeTheLock.Open();
         (await _theLateWorkTookTheLock.TryAwaitPassedThroughCountEqualToAsync(1, WaitTimeout.Milliseconds(200))).Must().BeFalse();

         theOtherHolderReleases.Open();
         (await _theLateWorkTookTheLock.TryAwaitPassedThroughCountEqualToAsync(1, WaitTimeout.Seconds(5))).Must().BeTrue();
         await Task.WhenAll(otherHolder, lateWork);
      }
   }

   public class Whose_action_throws : ThrowOnReentrancyAsyncLock_specification
   {
      [XF] public void Locked_lets_the_exception_through_and_releases_the_lock()
      {
         Invoking(() => _lock.Locked(() => throw new InvalidOperationException("the action failed"))).Must().Throw<InvalidOperationException>();
         _lock.Locked(() => 42).Must().Be(42);
      }

      [XF] public async Task LockedAsync_lets_the_exception_through_and_releases_the_lock()
      {
         await InvokingAsync(async () => await _lock.LockedAsync(async () =>
         {
            await Task.Yield();
            throw new InvalidOperationException("the action failed");
         })).Must().ThrowAsync<InvalidOperationException>();
         (await _lock.LockedAsync(() => Task.FromResult(42))).Must().Be(42);
      }
   }

   public class Taken_while_another_flow_holds_it_past_its_timeout : ThrowOnReentrancyAsyncLock_specification
   {
      readonly IThrowOnReentrancyAsyncLock _lockWithAShortTimeout = IThrowOnReentrancyAsyncLock.New(LockTimeout.Milliseconds(50));
      readonly IThreadGate _theHolderReleases = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "theHolderReleases");

      [XF] public async Task LockedAsync_throws_AsyncLockTimeoutException()
      {
         var holder = TaskCE.RunOnDedicatedThreadAsync(() => _lockWithAShortTimeout.Locked(_theHolderReleases.AwaitPassThrough));
         await _theHolderReleases.AwaitQueueLengthEqualToAsync(1);

         await InvokingAsync(async () => await _lockWithAShortTimeout.LockedAsync(async () => await Task.Yield())).Must().ThrowAsync<AsyncLockTimeoutException>();

         _theHolderReleases.Open();
         await holder;
      }

      [XF] public async Task Locked_throws_AsyncLockTimeoutException()
      {
         var holder = TaskCE.RunOnDedicatedThreadAsync(() => _lockWithAShortTimeout.Locked(_theHolderReleases.AwaitPassThrough));
         await _theHolderReleases.AwaitQueueLengthEqualToAsync(1);

         Invoking(() => _lockWithAShortTimeout.Locked(() => {})).Must().Throw<AsyncLockTimeoutException>();

         _theHolderReleases.Open();
         await holder;
      }
   }
}
