using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.ICriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
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
      readonly ManualResetEventSlim _threadIsBlocking = new(false);
      readonly ManualResetEventSlim _threadCompleted = new(false);
      readonly ManualResetEventSlim _lockHolderCanRelease = new(false);
      Exception? _thrownException;

      public When_a_thread_blocked_in_TakeLock_is_interrupted()
      {
         _criticalSection = _factory.Create(LockTimeout.Seconds(30));

         // Hold the lock so the other thread will block
         using var holdingLock = _criticalSection.TakeLock();

         var blockedThread = new Thread(() =>
         {
            try
            {
               _threadIsBlocking.Set();
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
               _threadCompleted.Set();
            }
         }) { IsBackground = true };

         blockedThread.Start();
         _threadIsBlocking.Wait();
         Thread.Sleep(50.Milliseconds());
         blockedThread.Interrupt();
         _threadCompleted.Wait(5.Seconds()).Must().BeTrue();
      }

      [ICriticalSectionMatrix] public void throws_ThreadInterruptedException() =>
         _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();

      [ICriticalSectionMatrix] public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         using(_criticalSection.TakeLock(LockTimeout.Seconds(1))) {}
      }
   }
}
