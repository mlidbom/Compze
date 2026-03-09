using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;
// ReSharper disable InconsistentNaming

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications;

[Collection(nameof(NonParallelCollection))]
public class ILock_specification : UniversalTestBase
{
   readonly LockFactory<ILock_specification> _lockFactory = new();
   readonly TestingTaskRunner _runner = new(timeout: 30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _lockFactory.Dispose();
   }

   public class Locked_with_Func : ILock_specification
   {
      [PCTLock] public void returns_the_value_from_the_function()
      {
         var @lock = _lockFactory.CreateLock();
         @lock.Locked(() => 42).Must().Be(42);
      }

      [PCTLock] public void propagates_exceptions_from_the_function()
      {
         var @lock = _lockFactory.CreateLock();
         Invoking(() => @lock.Locked<int>(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class Locked_with_Action : ILock_specification
   {
      [PCTLock] public void executes_the_action()
      {
         var @lock = _lockFactory.CreateLock();
         var executed = false;
         @lock.Locked(() => executed = true);
         executed.Must().BeTrue();
      }

      [PCTLock] public void propagates_exceptions_from_the_function()
      {
         var @lock = _lockFactory.CreateLock();
         Invoking(() => @lock.Locked(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class TakeLock : ILock_specification
   {
      [PCTLock] public void provides_mutual_exclusion_across_threads()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));
         var insideLockGate = ThreadGate.Closed(WaitTimeout.Seconds(30), "insideLock");

         _runner.Run(
            () => @lock.Locked(() => insideLockGate.AwaitPassThrough()),
            () => @lock.Locked(() => insideLockGate.AwaitPassThrough()));

         insideLockGate.AwaitQueueLengthEqualTo(1);
         insideLockGate.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockGate.Open();
         insideLockGate.AwaitPassedThroughCountEqualTo(2);
      }

      [PCTLock] public void supports_reentrant_locking_from_the_same_thread()
      {
         var @lock = _lockFactory.CreateLock();
         var result = @lock.Locked(() => @lock.Locked(() => 42));
         result.Must().Be(42);
      }

      [PCTLock] public void releases_the_lock_even_when_the_function_throws()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));

         try { @lock.Locked<int>(() => throw new InvalidOperationException()); }
         catch(InvalidOperationException) {}

         TaskCE.Run(() => @lock.Locked(() => {})).Wait();
      }

      [PCTLock] public void owning_thread_can_reenter_and_lock_is_only_released_when_outermost_lock_is_disposed()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(1));
         using(@lock.TakeLock())
         {
            using(@lock.TakeLock()) {}

            Invoking(() => TaskCE.Run(() => @lock.TakeLock(LockTimeout.Seconds(.1))).Wait())
              .Must().Throw<Exception>();
         }

         TaskCE.Run(() => @lock.TakeLock(LockTimeout.Milliseconds(0))).Wait();
      }
   }

   public class LockTimeout_property : ILock_specification
   {
      [PCTLock] public void defaults_to_LockTimeout_Default_when_no_timeout_is_specified()
      {
         var @lock = _lockFactory.CreateLock();
         @lock.LockTimeout.Must().Be(LockTimeout.Default);
      }

      [PCTLock] public void returns_the_timeout_specified_at_creation()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(7));
         @lock.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class ContentionCount : ILock_specification
   {
      [PCTLock] public void is_zero_when_no_contention_occurs()
      {
         var @lock = _lockFactory.CreateLock();

         using(@lock.TakeLock()) {}

         using(@lock.TakeLock()) {}

         @lock.ContentionCount.Must().Be(0L);
      }

      [PCTLock] public void increments_when_another_thread_contends_for_the_lock()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));

         var blockingLock = @lock.TakeLock();

         _runner.Run(() =>
         {
            using(@lock.TakeLock()) {}
         });

         SpinWait.SpinUntil(() => @lock.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();
      }
   }

   public class An_exception_is_thrown_by_TakeLock_if_lock_is_not_acquired_within_timeout : ILock_specification
   {
      [PCTLock] public void Exception_is_TakeLockTimeoutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), LockTimeout.Milliseconds(10), WaitTimeout.Seconds(30)).Must().BeAssignableTo<TakeLockTimeoutException>();

      [PCTLock] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 50.Milliseconds(), LockTimeout.Milliseconds(15), WaitTimeout.Seconds(30)).Message.Must().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [PCTLock] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), LockTimeout.Milliseconds(5), WaitTimeout.Milliseconds(1)).Message.Must().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      Exception RunScenario(TimeSpan ownerThreadBlockTime, LockTimeout monitorTimeout, WaitTimeout? timeToWaitForStackTrace = null)
      {
         var @lock = _lockFactory.CreateLock(monitorTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
#pragma warning disable CS0618 // Type or member is obsolete
            ((ILockInternals)@lock).SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
#pragma warning restore CS0618 // Type or member is obsolete
         }

         using var threadOneHasTakenLock = new ManualResetEvent(false);
         using var threadTwoIsAboutToTryToTakeLock = new ManualResetEvent(false);

         TaskCE.Run(() =>
         {
            var takenLock = @lock.TakeLock();
            threadOneHasTakenLock.Set();
            threadTwoIsAboutToTryToTakeLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(takenLock);
         });

         threadOneHasTakenLock.WaitOne();

         var thrownException = Invoking(() => TaskCE.Run(() =>
                                                     {
                                                        threadTwoIsAboutToTryToTakeLock.Set();
                                                        @lock.TakeLock();
                                                     })
                                                    .Wait())
                              .Must().Throw<AggregateException>()
                              .Which.InnerExceptions.Single();

         return thrownException;
      }
   }
}
