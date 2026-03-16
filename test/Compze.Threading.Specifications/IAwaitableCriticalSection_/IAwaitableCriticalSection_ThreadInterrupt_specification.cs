using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_;

[Collection(nameof(NonParallelCollection))]
public class IAwaitableCriticalSection_ThreadInterrupt_specification : UniversalTestBase
{
   readonly IAwaitableCriticalSectionMatrixAttribute.Factory<IAwaitableCriticalSection_ThreadInterrupt_specification> _factory = new();

   protected override void DisposeInternal() => _factory.Dispose();

   static (Exception? ThrownException, IAwaitableCriticalSection CriticalSection) InterruptThreadBlockedIn(
      IAwaitableCriticalSection criticalSection, Action<IAwaitableCriticalSection> blockingCall)
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
         //We need to capture whatever exception Thread.Interrupt causes to assert on it
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
      blockedThread.Interrupt();
      threadCompleted.AwaitPassedThroughCountEqualTo(1);

      return (thrownException, criticalSection);
   }

   public class When_a_thread_blocked_in_TakeUpdateLock_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var holdingLock = _criticalSection.TakeUpdateLock();
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TakeUpdateLock());
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_blocked_in_TakeReadLock_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var holdingLock = _criticalSection.TakeUpdateLock();
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TakeReadLock());
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TakeUpdateLockWhen_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TakeUpdateLockWhen(() => false));
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TakeReadLockWhen_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TakeReadLockWhen(() => false));
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TryTakeUpdateLockWhen_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TryTakeUpdateLockWhen(() => false));
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TryTakeReadLockWhen_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TryTakeReadLockWhen(() => false));
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }

   public class When_a_thread_waiting_in_TryAwait_is_interrupted : IAwaitableCriticalSection_ThreadInterrupt_specification
   {
      Exception? _thrownException;
      IAwaitableCriticalSection _criticalSection = null!;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         (_thrownException, _criticalSection) = InterruptThreadBlockedIn(_criticalSection, cs => cs.TryAwait(() => false));
      }

      [IAwaitableCriticalSectionMatrix] public void throws_ThreadInterruptedException()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();
      }

      [IAwaitableCriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeUpdateLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }
}
