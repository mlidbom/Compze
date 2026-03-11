using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications;

[Collection(nameof(NonParallelCollection))]
public class IAwaitableLock_specification : UniversalTestBase
{
   readonly AwaitableLockFactory<IAwaitableLock_specification> _lockFactory = new();
   readonly TestingTaskRunner _runner = TestingTaskRunner.WithTimeout(30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _lockFactory.Dispose();
   }

   [PCTAwaitableLock] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
   {
      var @lock = _lockFactory.CreateAwaitableLock(LockTimeout.Seconds(30));
      var insideLockSection = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(30), "insideLock");

      _runner.Run(
         () =>
         {
            using(@lock.TakeUpdateLock()) insideLockSection.Enter().Dispose();
         },
         () =>
         {
            using(@lock.TakeUpdateLock()) insideLockSection.Enter().Dispose();
         });

      insideLockSection.LetOneThreadEnterAndReachExit();
      insideLockSection.EntranceGate.TryAwaitQueueLengthEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse();
      insideLockSection.Open();
   }

   public class LockTimeout_property : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void defaults_to_LockTimeout_Default_when_no_timeout_is_specified()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         @lock.LockTimeout.Must().Be(LockTimeout.Default);
      }

      [PCTAwaitableLock] public void returns_the_timeout_specified_at_creation()
      {
         var @lock = _lockFactory.CreateAwaitableLock(LockTimeout.Seconds(7));
         @lock.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class WaitTimeout_property : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void defaults_to_WaitTimeout_Default_when_no_timeout_is_specified()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         @lock.WaitTimeout.Must().Be(WaitTimeout.Default);
      }

      [PCTAwaitableLock] public void returns_the_timeout_specified_at_creation()
      {
         var @lock = _lockFactory.CreateAwaitableLock(WaitTimeout.Seconds(7));
         @lock.WaitTimeout.Must().Be(WaitTimeout.Seconds(7));
      }
   }

   public class An_exception_is_thrown_by_TakeUpdateLock_if_lock_is_not_acquired_within_timeout : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void Exception_is_TakeLockTimeoutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), LockTimeout.Milliseconds(10), WaitTimeout.Seconds(30)).Must().BeAssignableTo<TakeLockTimeoutException>();

      [PCTAwaitableLock] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 50.Milliseconds(), LockTimeout.Milliseconds(15), WaitTimeout.Seconds(30)).Message.Must().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [PCTAwaitableLock] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), LockTimeout.Milliseconds(5), WaitTimeout.Milliseconds(1)).Message.Must().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      Exception RunScenario(TimeSpan ownerThreadBlockTime, LockTimeout lockTimeout, WaitTimeout? timeToWaitForStackTrace = null)
      {
         var @lock = _lockFactory.CreateAwaitableLock(lockTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
#pragma warning disable CS0618 // Type or member is obsolete
            ((ILockInternals)@lock).SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
#pragma warning restore CS0618 // Type or member is obsolete
         }

         using var threadOneHasTakenUpdateLock = new ManualResetEvent(false);
         using var threadTwoIsAboutToTryToTakeUpdateLock = new ManualResetEvent(false);

         TaskCE.Run(() =>
         {
            var takenLock = @lock.TakeUpdateLock();
            threadOneHasTakenUpdateLock.Set();
            threadTwoIsAboutToTryToTakeUpdateLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(takenLock);
         });

         threadOneHasTakenUpdateLock.WaitOne();

         var thrownException = Invoking(() => TaskCE.Run(() =>
                                                     {
                                                        threadTwoIsAboutToTryToTakeUpdateLock.Set();
                                                        @lock.TakeUpdateLock();
                                                     })
                                                    .Wait())
                              .Must().Throw<AggregateException>()
                              .Which.InnerExceptions.Single();

         return thrownException;
      }
   }

   public class TakeUpdateLockWhen : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void Returns_lock_when_condition_is_immediately_true()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         using var taken = @lock.TakeUpdateLockWhen(() => true);
         taken.Must().NotBeNull();
      }

      [PCTAwaitableLock] public void Waits_until_condition_becomes_true()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         var conditionMet = false;
         var lockAcquired = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "afterLockAcquired");

         _runner.Run(() =>
         {
            using(@lock.TakeUpdateLockWhen(() => conditionMet))
            {
               lockAcquired.AwaitPassThrough();
            }
         });

         lockAcquired.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         @lock.Update(() => conditionMet = true);
         lockAcquired.AwaitPassedThroughCountEqualTo(1);
      }

      [PCTAwaitableLock] public void Throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true() =>
         Invoking(() => _lockFactory.CreateAwaitableLock(WaitTimeout.Milliseconds(100))
                                    .TakeUpdateLockWhen(() => false))
           .Must().Throw<AwaitingConditionTimeoutException>();
   }

   public class TakeReadLockWhen : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void Returns_lock_when_condition_is_immediately_true()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         using var taken = @lock.TakeReadLockWhen(() => true);
         taken.Must().NotBeNull();
      }

      [PCTAwaitableLock] public void Throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true() =>
         Invoking(() => _lockFactory.CreateAwaitableLock(WaitTimeout.Milliseconds(100))
                                    .TakeReadLockWhen(() => false))
           .Must().Throw<AwaitingConditionTimeoutException>();
   }

   public class TryTakeReadLockWhen : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void Returns_lock_when_condition_is_immediately_true()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         using var taken = @lock.TryTakeReadLockWhen(() => true);
         taken.Must().NotBeNull();
      }

      [PCTAwaitableLock] public void Returns_null_when_condition_never_becomes_true()
      {
         var @lock = _lockFactory.CreateAwaitableLock(WaitTimeout.Milliseconds(100));
         var result = @lock.TryTakeReadLockWhen(() => false);
         result.Must().BeNull();
      }
   }

   public class TryAwait : IAwaitableLock_specification
   {
      [PCTAwaitableLock] public void Returns_true_when_condition_is_immediately_true()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         @lock.TryAwait(() => true).Must().BeTrue();
      }

      [PCTAwaitableLock] public void Returns_true_when_condition_becomes_true_within_timeout()
      {
         var @lock = _lockFactory.CreateAwaitableLock();
         var conditionMet = false;
         var awaitCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "awaitCompleted");

         _runner.Run(() =>
         {
            @lock.TryAwait(() => conditionMet).Must().BeTrue();
            awaitCompleted.AwaitPassThrough();
         });

         awaitCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         @lock.Update(() => conditionMet = true);
         awaitCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [PCTAwaitableLock] public void Returns_false_when_condition_never_becomes_true_within_timeout()
      {
         var @lock = _lockFactory.CreateAwaitableLock(WaitTimeout.Milliseconds(100));
         @lock.TryAwait(() => false).Must().BeFalse();
      }
   }
}
