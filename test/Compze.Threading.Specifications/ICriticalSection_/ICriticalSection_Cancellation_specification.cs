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
public class ICriticalSection_Cancellation_specification : UniversalTestBase
{
   readonly ICriticalSectionCancellationMatrixAttribute.CancellationFactory<ICriticalSection_Cancellation_specification> _factory = new();

   protected override void DisposeInternal() => _factory.Dispose();

   public class When_a_thread_blocked_in_TakeLock_is_cancelled : ICriticalSection_Cancellation_specification
   {
      ICriticalSection _criticalSection = null!;
      Exception? _thrownException;

      void RunScenario()
      {
         if(_thrownException != null || _criticalSection != null!) return;

         _criticalSection = _factory.Create(LockTimeout.Seconds(30));
         using var cancellationTrigger = _factory.CreateCancellationTrigger();

         var aboutToBlock = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "aboutToBlock");
         var threadCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "threadCompleted");

         // Hold the lock so the other thread will block
         using var holdingLock = _criticalSection.TakeLock();

         var blockedThread = new Thread(() =>
         {
            try
            {
               aboutToBlock.AwaitPassThrough();
               _criticalSection.TakeLock(cancellationTrigger.Token);
            }
#pragma warning disable CA1031
            //We need to capture whatever exception cancellation causes to assert on it
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
         cancellationTrigger.Cancel(blockedThread);
         threadCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [ICriticalSectionCancellationMatrix]
      public void throws_expected_cancellation_exception()
      {
         RunScenario();
         _thrownException.Must().NotBeNull().Satisfy(ex => ex.GetType() == _factory.ExpectedExceptionType);
      }

      [ICriticalSectionCancellationMatrix]
      public void lock_is_not_orphaned_and_other_threads_can_acquire_it()
      {
         RunScenario();
         using(_criticalSection.TakeLock(timeout: LockTimeout.Seconds(1))) {}
      }
   }
}
