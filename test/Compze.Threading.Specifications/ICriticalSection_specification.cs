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
   readonly CriticalSectionFactory<ILock_specification> _lockFactory = new();
   readonly TestingTaskRunner _runner = new(timeout: 30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _lockFactory.Dispose();
   }

   public class Locked_with_Func : ILock_specification
   {
      [ICriticalSectionMatrix] public void returns_the_value_from_the_function()
      {
         var criticalSection = _lockFactory.CreateLock();
         criticalSection.Locked(() => 42).Must().Be(42);
      }

      [ICriticalSectionMatrix] public void propagates_exceptions_from_the_function()
      {
         var criticalSection = _lockFactory.CreateLock();
         Invoking(() => criticalSection.Locked<int>(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class Locked_with_Action : ILock_specification
   {
      [ICriticalSectionMatrix] public void executes_the_action()
      {
         var criticalSection = _lockFactory.CreateLock();
         var executed = false;
         criticalSection.Locked(() => executed = true);
         executed.Must().BeTrue();
      }

      [ICriticalSectionMatrix] public void propagates_exceptions_from_the_function()
      {
         var criticalSection = _lockFactory.CreateLock();
         Invoking(() => criticalSection.Locked(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class TakeLock : ILock_specification
   {
      [ICriticalSectionMatrix] public void provides_mutual_exclusion_across_threads()
      {
         var criticalSection = _lockFactory.CreateLock(LockTimeout.Seconds(30));
         var insideLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "insideLock");

         _runner.Run(
            () => criticalSection.Locked(insideLockGate.AwaitPassThrough),
            () => criticalSection.Locked(insideLockGate.AwaitPassThrough));

         insideLockGate.AwaitQueueLengthEqualTo(1);
         insideLockGate.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockGate.Open();
         insideLockGate.AwaitPassedThroughCountEqualTo(2);
      }

      [ICriticalSectionMatrix] public void supports_reentrant_locking_from_the_same_thread()
      {
         var criticalSection = _lockFactory.CreateLock();
         var result = criticalSection.Locked(() => criticalSection.Locked(() => 42));
         result.Must().Be(42);
      }

      [ICriticalSectionMatrix] public void releases_the_lock_even_when_the_function_throws()
      {
         var criticalSection = _lockFactory.CreateLock(LockTimeout.Seconds(30));

         try { criticalSection.Locked<int>(() => throw new InvalidOperationException()); }
         catch(InvalidOperationException) {}

         TaskCE.Run(() => criticalSection.Locked(() => {})).Wait();
      }

      [ICriticalSectionMatrix] public void owning_thread_can_reenter_and_lock_is_only_released_when_outermost_lock_is_disposed()
      {
         var criticalSection = _lockFactory.CreateLock(LockTimeout.Seconds(1));
         using(criticalSection.TakeLock())
         {
            using(criticalSection.TakeLock()) {}

            Invoking(() => TaskCE.Run(() => criticalSection.TakeLock(LockTimeout.Seconds(.1))).Wait())
              .Must().Throw<Exception>();
         }

         TaskCE.Run(() => criticalSection.TakeLock(LockTimeout.Milliseconds(0))).Wait();
      }
   }

   public class LockTimeout_property : ILock_specification
   {
      [ICriticalSectionMatrix] public void defaults_to_LockTimeout_Default_when_no_timeout_is_specified()
      {
         var criticalSection = _lockFactory.CreateLock();
         criticalSection.LockTimeout.Must().Be(LockTimeout.Default);
      }

      [ICriticalSectionMatrix] public void returns_the_timeout_specified_at_creation()
      {
         var criticalSection = _lockFactory.CreateLock(LockTimeout.Seconds(7));
         criticalSection.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class ContentionCount : ILock_specification
   {
      [ICriticalSectionMatrix] public void is_zero_when_no_contention_occurs()
      {
         var criticalSection = _lockFactory.CreateLock();

         using(criticalSection.TakeLock()) {}

         using(criticalSection.TakeLock()) {}

         criticalSection.ContentionCount.Must().Be(0L);
      }

      [ICriticalSectionMatrix] public void increments_when_another_thread_contends_for_the_lock()
      {
         var criticalSection = _lockFactory.CreateLock(LockTimeout.Seconds(30));

         var blockingLock = criticalSection.TakeLock();

         _runner.Run(() =>
         {
            using(criticalSection.TakeLock()) {}
         });

         SpinWait.SpinUntil(() => criticalSection.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();
      }
   }

   public class An_exception_is_thrown_by_TakeLock_if_lock_is_not_acquired_within_timeout : ILock_specification
   {
      [ICriticalSectionMatrix] public void Exception_is_TakeLockTimeoutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), LockTimeout.Milliseconds(10), WaitTimeout.Seconds(30)).Must().BeAssignableTo<TakeLockTimeoutException>();

      [ICriticalSectionMatrix] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 50.Milliseconds(), LockTimeout.Milliseconds(15), WaitTimeout.Seconds(30)).Message.Must().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [ICriticalSectionMatrix] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), LockTimeout.Milliseconds(5), WaitTimeout.Milliseconds(1)).Message.Must().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      Exception RunScenario(TimeSpan ownerThreadBlockTime, LockTimeout monitorTimeout, WaitTimeout? timeToWaitForStackTrace = null)
      {
         var criticalSection = _lockFactory.CreateLock(monitorTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
#pragma warning disable CS0618 // Type or member is obsolete
            ((ILockInternals)criticalSection).SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
#pragma warning restore CS0618 // Type or member is obsolete
         }

         using var threadOneHasTakenLock = new ManualResetEvent(false);
         using var threadTwoIsAboutToTryToTakeLock = new ManualResetEvent(false);

         TaskCE.Run(() =>
         {
            var takenLock = criticalSection.TakeLock();
            threadOneHasTakenLock.Set();
            threadTwoIsAboutToTryToTakeLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(takenLock);
         });

         threadOneHasTakenLock.WaitOne();

         var thrownException = Invoking(() => TaskCE.Run(() =>
                                                     {
                                                        threadTwoIsAboutToTryToTakeLock.Set();
                                                        criticalSection.TakeLock();
                                                     })
                                                    .Wait())
                              .Must().Throw<AggregateException>()
                              .Which.InnerExceptions.Single();

         return thrownException;
      }
   }
}
