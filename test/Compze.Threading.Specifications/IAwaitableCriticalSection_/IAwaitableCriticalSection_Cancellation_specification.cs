using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_;

[Collection(nameof(NonParallelCollection))]
public class IAwaitableCriticalSection_Cancellation_specification : UniversalTestBase
{
   readonly IAwaitableCriticalSectionCancellationMatrixAttribute.CancellationFactory<IAwaitableCriticalSection_Cancellation_specification> _factory = new();

   protected override void DisposeInternal() => _factory.Dispose();

   static (Exception? ThrownException, IAwaitableCriticalSection CriticalSection) CancelThreadBlockedIn(
      IAwaitableCriticalSection criticalSection, CancellationTrigger cancellationTrigger, Action<IAwaitableCriticalSection> blockingCall)
   {
      var aboutToBlock = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "aboutToBlock");
      var threadCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "threadCompleted");
      Exception? thrownException = null;

      var blockedThread = new Thread(() =>
      {
         try
         {
            aboutToBlock.AwaitPassThrough();
            blockingCall(criticalSection);
         }
#pragma warning disable CA1031
         //We need to capture whatever exception cancellation causes to assert on it
         catch(Exception ex)
         {
#pragma warning restore CA1031
            thrownException = ex;
         }
         finally
         {
            threadCompleted.AwaitPassThrough();
         }
      }) { IsBackground = true };

      blockedThread.Start();
      aboutToBlock.AwaitPassedThroughCountEqualTo(1);
      Thread.Sleep(50.Milliseconds());
      cancellationTrigger.Cancel(blockedThread);
      threadCompleted.AwaitPassedThroughCountEqualTo(1);

      return (thrownException, criticalSection);
   }

   public class When_a_thread_blocked_in_TakeUpdateLock_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         using var holdingLock = _criticalSection.TakeUpdateLock();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TakeUpdateLock(cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_blocked_in_TakeReadLock_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         using var holdingLock = _criticalSection.TakeUpdateLock();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TakeReadLock(cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TakeUpdateLockWhen_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TakeUpdateLockWhen(() => false, cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TakeReadLockWhen_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TakeReadLockWhen(() => false, cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TryTakeUpdateLockWhen_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TryTakeUpdateLockWhen(() => false, cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TryTakeReadLockWhen_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TryTakeReadLockWhen(() => false, cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TryAwait_is_cancelled : IAwaitableCriticalSection_Cancellation_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();
         (_thrownException, _criticalSection) = CancelThreadBlockedIn(_criticalSection, cancellationTrigger, cs => cs.TryAwait(() => false, cancellationTrigger.Token));
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [IAwaitableCriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }
}
