using Compze.Internals.SystemCE;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.ICriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.ICriticalSection_;

[Collection(nameof(NonParallelCollection))]
public class ICriticalSection_ThreadInterrupt_specification : UniversalTestBase
{
   readonly ICriticalSectionMatrixAttribute.Factory<ICriticalSection_ThreadInterrupt_specification> _factory = new();

   protected override void DisposeInternal() => _factory.Dispose();

   public class When_a_thread_blocked_in_TakeLock_is_interrupted : ICriticalSection_ThreadInterrupt_specification
   {
      readonly ICriticalSection _criticalSection;
      Exception? _thrownException;

      public When_a_thread_blocked_in_TakeLock_is_interrupted()
      {
         _criticalSection = _factory.Create(LockTimeout.Seconds(30));

         var aboutToBlock = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "aboutToBlock");
         var threadCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "threadCompleted");

         // Hold the lock so the other thread will block
         using var holdingLock = _criticalSection.TakeLock();

         var blockedThread = new Thread(() =>
         {
            try
            {
               aboutToBlock.AwaitPassThrough();
               _criticalSection.TakeLock();
            }
#pragma warning disable CA1031
            //We need to capture whatever exception Thread.Interrupt causes to assert on it
            catch(Exception ex)
            {
#pragma warning restore CA1031
               _thrownException = ex;
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
      }

      [ICriticalSectionMatrix] public void throws_ThreadInterruptedException() =>
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();

      [ICriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         using(_criticalSection.TakeLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }
}
