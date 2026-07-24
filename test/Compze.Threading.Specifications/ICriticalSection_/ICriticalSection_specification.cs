using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.ICriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.ICriticalSection_;

[Collection(nameof(NonParallelCollection))]
public class ICriticalSection_specification : UniversalTestBase
{
   readonly ICriticalSectionMatrixAttribute.Factory<ICriticalSection_specification> _factory = new();
   readonly TestingTaskRunner _runner = new(timeout: 30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _factory.Dispose();
   }

   public class Locked_with_Func : ICriticalSection_specification
   {
      [ICriticalSectionMatrix] public void returns_the_value_from_the_function()
      {
         var criticalSection = _factory.Create();
         criticalSection.Locked(() => 42).Must().Be(42);
      }

      [ICriticalSectionMatrix] public void propagates_exceptions_from_the_function()
      {
         var criticalSection = _factory.Create();
         Invoking(() => criticalSection.Locked<int>(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class Locked_with_Action : ICriticalSection_specification
   {
      [ICriticalSectionMatrix] public void executes_the_action()
      {
         var criticalSection = _factory.Create();
         var executed = false;
         criticalSection.Locked(() => executed = true);
         executed.Must().BeTrue();
      }

      [ICriticalSectionMatrix] public void propagates_exceptions_from_the_function()
      {
         var criticalSection = _factory.Create();
         Invoking(() => criticalSection.Locked(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class TakeLock : ICriticalSection_specification
   {
      [ICriticalSectionMatrix] public void provides_mutual_exclusion_across_threads()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(30));
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
         var criticalSection = _factory.Create();
         var result = criticalSection.Locked(() => criticalSection.Locked(() => 42));
         result.Must().Be(42);
      }

      [ICriticalSectionMatrix] public void releases_the_lock_even_when_the_function_throws()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(30));

         try { criticalSection.Locked<int>(() => throw new InvalidOperationException()); }
         catch(InvalidOperationException) {}

         TaskCE.Run(() => criticalSection.Locked(() => {})).Wait();
      }

      [ICriticalSectionMatrix] public void owning_thread_can_reenter_and_lock_is_only_released_when_outermost_lock_is_disposed()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(1));
         using(criticalSection.TakeLock())
         {
            using(criticalSection.TakeLock()) {}

            Invoking(() => TaskCE.Run(() => criticalSection.TakeLock(timeout: LockTimeout.Seconds(.1))).Wait())
              .Must().Throw<Exception>();
         }

         TaskCE.Run(() => criticalSection.TakeLock(timeout: LockTimeout.Milliseconds(0))).Wait();
      }
   }

   public class LockTimeout_property : ICriticalSection_specification
   {
      [ICriticalSectionMatrix] public void defaults_to_LockTimeout_Default_when_no_timeout_is_specified() =>
         _factory.Create().LockTimeout.Must().Be(LockTimeout.Default);

      [ICriticalSectionMatrix] public void returns_the_timeout_specified_at_creation() =>
         _factory.Create(LockTimeout.Seconds(7)).LockTimeout.Must().Be(LockTimeout.Seconds(7));
   }

   public class ContentionCount : ICriticalSection_specification
   {
      [ICriticalSectionMatrix] public void is_zero_when_no_contention_occurs()
      {
         var criticalSection = _factory.Create();

         criticalSection.TakeLock().Dispose();
         criticalSection.TakeLock().Dispose();

         criticalSection.ContentionCount.Must().Be(0L);
      }

      [ICriticalSectionMatrix] public void increments_when_another_thread_contends_for_the_lock()
      {
         var criticalSection = _factory.Create(LockTimeout.Seconds(30));

         var blockingLock = criticalSection.TakeLock();

         _runner.Run(() =>
         {
            using(criticalSection.TakeLock()) {}
         });

         SpinWait.SpinUntil(() => criticalSection.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();
      }
   }

   public class An_exception_is_thrown_by_TakeLock_if_lock_is_not_acquired_within_timeout : ICriticalSection_specification
   {
      [ICriticalSectionMatrix] public void Exception_is_TakeLockTimeoutException()
      {
         var criticalSection = _factory.Create(LockTimeout.Milliseconds(10));
         using var lockHeldByAnotherThread = new LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt((ILockInternals)criticalSection, () => criticalSection.TakeLock());
         lockHeldByAnotherThread.TheAcquisitionTimeoutException.Must().BeAssignableTo<TakeLockTimeoutException>();
      }

      [ICriticalSectionMatrix] public void When_the_holder_disposes_the_lock_within_the_stack_trace_fetch_timeout_the_exception_message_contains_the_holders_disposal_stack_trace()
      {
         var criticalSection = _factory.Create(LockTimeout.Milliseconds(10));
         using var lockHeldByAnotherThread = new LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt((ILockInternals)criticalSection, () => criticalSection.TakeLock(), stackTraceFetchTimeout: WaitTimeout.Seconds(30));
         lockHeldByAnotherThread.ReleaseTheHeldLock();
         lockHeldByAnotherThread.TheAcquisitionTimeoutException.Message.Must().Contain(nameof(LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt.DisposeInMethodSoItWillBeInTheCapturedCallStack));
      }

      [ICriticalSectionMatrix] public void While_the_holder_still_holds_the_lock_the_exception_message_reports_it_could_not_fetch_the_holders_stack_trace()
      {
         var criticalSection = _factory.Create(LockTimeout.Milliseconds(10));
         using var lockHeldByAnotherThread = new LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt((ILockInternals)criticalSection, () => criticalSection.TakeLock(), stackTraceFetchTimeout: WaitTimeout.Milliseconds(50));
         //Read while the holder still holds the lock (this scenario is not released until it is disposed, below this line):
         //the fetch has no disposal stack trace to find and times out - deterministically, because the holder provably does
         //not dispose during the fetch window.
         lockHeldByAnotherThread.TheAcquisitionTimeoutException.Message.Must().NotContain(nameof(LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt.DisposeInMethodSoItWillBeInTheCapturedCallStack));
      }
   }
}
