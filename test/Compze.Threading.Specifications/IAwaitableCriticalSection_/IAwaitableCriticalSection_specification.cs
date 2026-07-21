using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_;

[Collection(nameof(NonParallelCollection))]
public class IAwaitableCriticalSection_specification : UniversalTestBase
{
   readonly IAwaitableCriticalSectionMatrixAttribute.Factory<IAwaitableCriticalSection_specification> _factory = new();
   readonly TestingTaskRunner _runner = TestingTaskRunner.WithTimeout(30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _factory.Dispose();
   }

   [IAwaitableCriticalSectionMatrix] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
   {
      var criticalSection = _factory.Create(LockTimeout.Seconds(30));
      var insideLock = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "insideLock");

      _runner.Run(
         () =>
         {
            using(criticalSection.TakeUpdateLock()) insideLock.AwaitPassThrough();
         },
         () =>
         {
            using(criticalSection.TakeUpdateLock()) insideLock.AwaitPassThrough();
         });

      // One thread acquired the lock and is now blocked at the closed gate — still holding the lock
      insideLock.AwaitQueueLengthEqualTo(1);

      // The other thread can't reach the gate because the lock blocks it
      insideLock.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();

      insideLock.Open();

      // Both threads pass through — the second was unblocked once the first released the lock
      insideLock.AwaitPassedThroughCountEqualTo(2);
   }

   public class TakeUpdateLock : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void owning_thread_can_reenter_and_lock_is_only_released_when_outermost_lock_is_disposed()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(1));
         using(criticalSection.TakeUpdateLock())
         {
            using(criticalSection.TakeUpdateLock()) {}

            Invoking(() => TaskCE.Run(() => criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(.1))).Wait())
              .Must().Throw<Exception>();
         }

         TaskCE.Run(() => criticalSection.TakeUpdateLock(timeout: LockTimeout.Milliseconds(0))).Wait();
      }
   }

   public class LockTimeout_property : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void defaults_to_LockTimeout_Default_when_no_timeout_is_specified()
      {
         var criticalSection = _factory.Create();
         criticalSection.LockTimeout.Must().Be(LockTimeout.Default);
      }

      [IAwaitableCriticalSectionMatrix] public void returns_the_timeout_specified_at_creation()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(7));
         criticalSection.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class WaitTimeout_property : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void defaults_to_WaitTimeout_Default_when_no_timeout_is_specified()
      {
         var criticalSection = _factory.Create();
         criticalSection.WaitTimeout.Must().Be(WaitTimeout.Default);
      }

      [IAwaitableCriticalSectionMatrix] public void returns_the_timeout_specified_at_creation()
      {
         var criticalSection = _factory.Create(WaitTimeout.Seconds(7));
         criticalSection.WaitTimeout.Must().Be(WaitTimeout.Seconds(7));
      }
   }

   public class An_exception_is_thrown_by_TakeUpdateLock_if_lock_is_not_acquired_within_timeout : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Exception_is_TakeLockTimeoutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), LockTimeout.Milliseconds(10), WaitTimeout.Seconds(30)).Must().BeAssignableTo<TakeLockTimeoutException>();

      [IAwaitableCriticalSectionMatrix] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 50.Milliseconds(), LockTimeout.Milliseconds(15), WaitTimeout.Seconds(30)).Message.Must().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [IAwaitableCriticalSectionMatrix] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), LockTimeout.Milliseconds(5), WaitTimeout.Milliseconds(1)).Message.Must().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      Exception RunScenario(TimeSpan ownerThreadBlockTime, LockTimeout lockTimeout, WaitTimeout? timeToWaitForStackTrace = null)
      {
         var criticalSection = _factory.Create(lockTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
#pragma warning disable CS0618 // Type or member is obsolete
            ((ILockInternals)criticalSection).SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
#pragma warning restore CS0618 // Type or member is obsolete
         }

         using var threadOneHasTakenUpdateLock = new ManualResetEvent(false);
         using var threadTwoIsAboutToTryToTakeUpdateLock = new ManualResetEvent(false);

         TaskCE.Run(() =>
         {
            var takenLock = criticalSection.TakeUpdateLock();
            threadOneHasTakenUpdateLock.Set();
            threadTwoIsAboutToTryToTakeUpdateLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(takenLock);
         });

         threadOneHasTakenUpdateLock.WaitOne();

         var thrownException = Invoking(() => TaskCE.Run(() =>
                                                     {
                                                        threadTwoIsAboutToTryToTakeUpdateLock.Set();
                                                        criticalSection.TakeUpdateLock();
                                                     })
                                                    .Wait())
                              .Must().Throw<AggregateException>()
                              .Which.InnerExceptions.Single();

         return thrownException;
      }
   }

   public class UpdateWhen_with_Action : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Blocks_until_the_condition_becomes_true_then_executes_the_action_within_the_update_lock()
      {
         var criticalSection = _factory.Create();
         var conditionMet = false;
         var actionExecuted = false;
         var updateWhenReturned = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "updateWhenReturned");

         _runner.Run(() =>
         {
            //A statement-block lambda so overload resolution picks the Action-taking UpdateWhen under specification - the expression form would bind to the Func overload.
            criticalSection.UpdateWhen(() => conditionMet, () => { actionExecuted = true; });
            updateWhenReturned.AwaitPassThrough();
         });

         updateWhenReturned.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         criticalSection.Update(() => conditionMet = true);
         updateWhenReturned.AwaitPassedThroughCountEqualTo(1);
         actionExecuted.Must().BeTrue();
      }
   }

   public class TakeUpdateLockWhen : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Returns_lock_when_condition_is_immediately_true()
      {
         var criticalSection = _factory.Create();
         using var taken = criticalSection.TakeUpdateLockWhen(() => true);
         taken.Must().NotBeNull();
      }

      [IAwaitableCriticalSectionMatrix] public void Waits_until_condition_becomes_true()
      {
         var criticalSection = _factory.Create();
         var conditionMet = false;
         var lockAcquired = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "afterLockAcquired");

         _runner.Run(() =>
         {
            using(criticalSection.TakeUpdateLockWhen(() => conditionMet))
            {
               lockAcquired.AwaitPassThrough();
            }
         });

         lockAcquired.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         criticalSection.Update(() => conditionMet = true);
         lockAcquired.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableCriticalSectionMatrix] public void Throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true() =>
         Invoking(() => _factory.Create(WaitTimeout.Milliseconds(100))
                                    .TakeUpdateLockWhen(() => false))
           .Must().Throw<AwaitingConditionTimeoutException>();

      [IAwaitableCriticalSectionMatrix] public void Releases_outer_lock_while_waiting_so_another_thread_can_make_condition_true()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var conditionMet = false;
         var innerLockAcquired = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "innerLockAcquired");

         _runner.Run(() =>
         {
            using(criticalSection.TakeUpdateLock())
            {
               using(criticalSection.TakeUpdateLockWhen(() => conditionMet))
               {
                  innerLockAcquired.AwaitPassThrough();
               }
            }
         });

         // The thread is waiting inside TakeUpdateLockWhen — it must have released the outer lock
         innerLockAcquired.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(200)).Must().BeFalse();

         // Another thread can acquire and update — proving the outer lock was released
         criticalSection.Update(() => conditionMet = true);

         // The waiting thread reacquires and proceeds
         innerLockAcquired.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableCriticalSectionMatrix] public void Outer_lock_is_still_held_after_inner_condition_lock_is_disposed()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var conditionMet = false;
         var innerLockDisposed = IThreadGate.NewClosed(WaitTimeout.Seconds(10), "innerLockDisposed");
         var outerLockStillExclusive = IThreadGate.NewClosed(WaitTimeout.Seconds(10), "outerLockStillExclusive");

         _runner.Run(() =>
         {
            using(criticalSection.TakeUpdateLock())
            {
               using(criticalSection.TakeUpdateLockWhen(() => conditionMet)) {}

               // Inner condition lock is disposed — but we still hold the outer lock
               innerLockDisposed.AwaitPassThrough();

               // Verify the outer lock is still held by blocking here while the test thread tries to acquire
               outerLockStillExclusive.AwaitPassThrough();
            }
         });

         // Make the condition true so the inner lock can be acquired
         criticalSection.Update(() => conditionMet = true);

         innerLockDisposed.Open();
         innerLockDisposed.AwaitPassedThroughCountEqualTo(1);

         // Try to acquire from this thread — should fail because the outer lock is still held
         Invoking(() => criticalSection.TakeUpdateLock(timeout: LockTimeout.Milliseconds(50)))
           .Must().Throw<Exception>();

         outerLockStillExclusive.Open();
      }
   }

   public class TakeReadLockWhen : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Returns_lock_when_condition_is_immediately_true()
      {
         var criticalSection = _factory.Create();
         using var taken = criticalSection.TakeReadLockWhen(() => true);
         taken.Must().NotBeNull();
      }

      [IAwaitableCriticalSectionMatrix] public void Throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true() =>
         Invoking(() => _factory.Create(WaitTimeout.Milliseconds(100))
                                    .TakeReadLockWhen(() => false))
           .Must().Throw<AwaitingConditionTimeoutException>();
   }

   public class TryTakeReadLockWhen : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Returns_lock_when_condition_is_immediately_true()
      {
         var criticalSection = _factory.Create();
         using var taken = criticalSection.TryTakeReadLockWhen(() => true);
         taken.Must().NotBeNull();
      }

      [IAwaitableCriticalSectionMatrix] public void Returns_null_when_condition_never_becomes_true()
      {
         var criticalSection = _factory.Create(WaitTimeout.Milliseconds(100));
         var result = criticalSection.TryTakeReadLockWhen(() => false);
         result.Must().BeNull();
      }
   }

   public class TryAwait : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Returns_true_when_condition_is_immediately_true()
      {
         var criticalSection = _factory.Create();
         criticalSection.TryAwait(() => true).Must().BeTrue();
      }

      [IAwaitableCriticalSectionMatrix] public void Returns_true_when_condition_becomes_true_within_timeout()
      {
         var criticalSection = _factory.Create();
         var conditionMet = false;
         var awaitCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "awaitCompleted");

         _runner.Run(() =>
         {
            criticalSection.TryAwait(() => conditionMet).Must().BeTrue();
            awaitCompleted.AwaitPassThrough();
         });

         awaitCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         criticalSection.Update(() => conditionMet = true);
         awaitCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableCriticalSectionMatrix] public void Returns_false_when_condition_never_becomes_true_within_timeout()
      {
         var criticalSection = _factory.Create(WaitTimeout.Milliseconds(100));
         criticalSection.TryAwait(() => false).Must().BeFalse();
      }
   }

   public class TryReadWhen : IAwaitableCriticalSection_specification
   {
      [IAwaitableCriticalSectionMatrix] public void Returns_true_and_the_value_when_condition_is_immediately_true()
      {
         var criticalSection = _factory.Create();
         criticalSection.TryReadWhen(() => true, () => 42, out var result).Must().BeTrue();
         result.Must().Be(42);
      }

      [IAwaitableCriticalSectionMatrix] public void Returns_true_and_the_value_when_condition_becomes_true_within_timeout()
      {
         var criticalSection = _factory.Create();
         var conditionMet = false;
         var readCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "readCompleted");

         _runner.Run(() =>
         {
            criticalSection.TryReadWhen(() => conditionMet, () => 99, out var value).Must().BeTrue();
            value.Must().Be(99);
            readCompleted.AwaitPassThrough();
         });

         readCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         criticalSection.Update(() => conditionMet = true);
         readCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableCriticalSectionMatrix] public void Returns_false_and_default_when_condition_never_becomes_true_within_timeout()
      {
         var criticalSection = _factory.Create(WaitTimeout.Milliseconds(100));
         criticalSection.TryReadWhen(() => false, () => 42, out var result).Must().BeFalse();
         result.Must().Be(0);
      }
   }
}
