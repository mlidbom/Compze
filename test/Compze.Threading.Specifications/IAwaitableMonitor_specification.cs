using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications;

///<summary>Monitor-specific tests that do not apply to polling-based implementations (reentrancy, thread interruption).</summary>
[Collection(nameof(NonParallelCollection))]
public class IAwaitableMonitor_specification : UniversalTestBase
{
   [XF] public void Owning_thread_can_reenter_the_lock_and_the_lock_is_only_exited_when_releasing_the_outermost_lock()
   {
      var monitor = IAwaitableMonitor.New(LockTimeout.Seconds(1));
      using(monitor.TakeUpdateLock())
      {
         using(monitor.TakeUpdateLock()) {}

         Invoking(() => TaskCE.Run(() => monitor.TakeUpdateLock(LockTimeout.Seconds(.1))).Wait())
           .Must().Throw<Exception>();
      }

      TaskCE.Run(() => monitor.TakeUpdateLock(LockTimeout.Milliseconds(0))).Wait();
   }

   public class When_a_thread_waiting_in_TakeUpdateLockWhen_is_interrupted : IAwaitableMonitor_specification
   {
      readonly IAwaitableMonitor _lock = IAwaitableMonitor.New(LockTimeout.Seconds(30));
      readonly ManualResetEventSlim _threadIsWaiting = new(false);
      readonly ManualResetEventSlim _threadCompleted = new(false);
      Exception? _thrownException;

      public When_a_thread_waiting_in_TakeUpdateLockWhen_is_interrupted()
      {
         var waitingThread = new Thread(() =>
         {
            try
            {
               _threadIsWaiting.Set();
               _lock.TakeUpdateLockWhen(() => false);
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

         waitingThread.Start();
         _threadIsWaiting.Wait();
         Thread.Sleep(50.Milliseconds());
         waitingThread.Interrupt();
         _threadCompleted.Wait(5.Seconds()).Must().BeTrue();
      }

      [XF] public void throws_ThreadInterruptedException() => _thrownException.Must().NotBeNull().BeExactType<ThreadInterruptedException>();

      [XF] public void lock_is_released_so_other_threads_can_acquire_it()
      {
         using(_lock.TakeUpdateLock(LockTimeout.Seconds(1))) {}
      }
   }
}
