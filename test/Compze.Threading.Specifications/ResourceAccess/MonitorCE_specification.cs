using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE;
using Compze.Threading.ResourceAccess;
using Compze.Threading.ResourceAccess.Exceptions;
using Compze.Must;
using Compze.xUnitBDD;
using NCrunch.Framework;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.ResourceAccess;

[Collection(nameof(NonParallelCollection))]
public class MonitorCE_specification : UniversalTestBase
{
   [XF] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
   {
      var monitor = IAwaitableLock.New(LockTimeout.Seconds(1));

      var updateLock = monitor.TakeUpdateLock();

      using var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
      using var otherThreadGotLock = new ManualResetEventSlim(false);
      var otherThreadTask = TaskCE.Run(() =>
      {
         otherThreadIsWaitingForLock.Set();
         using(monitor.TakeUpdateLock())
         {
            otherThreadGotLock.Set();
         }
      });

      otherThreadIsWaitingForLock.Wait();
      otherThreadGotLock.Wait(50.Milliseconds()).Must().BeFalse();

      updateLock.Dispose();
      otherThreadGotLock.Wait(50.Milliseconds()).Must().BeTrue();

      Task.WaitAll(otherThreadTask);
   }

   [XF] public void Owning_thread_can_reenter_the_lock_and_the_lock_is_only_exited_when_releasing_the_outermost_lock()
   {
      var monitor = IAwaitableLock.New(LockTimeout.Seconds(1));
      using(monitor.TakeUpdateLock())
      {
         using(monitor.TakeUpdateLock()) {}

         Invoking(() => TaskCE.Run(() => monitor.TakeUpdateLock(LockTimeout.Seconds(.1))).Wait())
           .Must().Throw<Exception>();
      }

      TaskCE.Run(() => monitor.TakeUpdateLock(LockTimeout.Milliseconds(0))).Wait();
   }

   public class When_a_thread_waiting_in_TakeUpdateLockWhen_is_interrupted : MonitorCE_specification
   {
      readonly IAwaitableLock _lock = IAwaitableLock.New(LockTimeout.Seconds(30));
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

   public class An_exception_is_thrown_by_EnterUpdateLock_if_lock_is_not_acquired_within_timeout : UniversalTestBase
   {
      [XF, EnableRdi(false)] public void Exception_is_ObjectLockTimedOutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), LockTimeout.Milliseconds(10), WaitTimeout.Seconds(30)).Must().BeAssignableTo<TakeLockTimeoutException>();

      [XF, EnableRdi(false)] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 50.Milliseconds(), LockTimeout.Milliseconds(15), WaitTimeout.Seconds(30)).Message.Must().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [XF] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), LockTimeout.Milliseconds(5), WaitTimeout.Milliseconds(1)).Message.Must().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      static Exception RunScenario(TimeSpan ownerThreadBlockTime, LockTimeout monitorTimeout, WaitTimeout? timeToWaitForStackTrace = null)
      {
         var monitor = IAwaitableLock.New(monitorTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
#pragma warning disable CS0618 // Type or member is obsolete
            ((ILockInternals)monitor).SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
#pragma warning restore CS0618 // Type or member is obsolete
         }

         using var threadOneHasTakenUpdateLock = new ManualResetEvent(false);
         using var threadTwoIsAboutToTryToEnterUpdateLock = new ManualResetEvent(false);

         TaskCE.Run(() =>
         {
            var @lock = monitor.TakeUpdateLock();
            threadOneHasTakenUpdateLock.Set();
            threadTwoIsAboutToTryToEnterUpdateLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(@lock);
         });

         threadOneHasTakenUpdateLock.WaitOne();

         var thrownException = Invoking(() => TaskCE.Run(() =>
                                                     {
                                                        threadTwoIsAboutToTryToEnterUpdateLock.Set();
                                                        monitor.TakeUpdateLock();
                                                     })
                                                    .Wait())
                              .Must().Throw<AggregateException>()
                              .Which.InnerExceptions.Single();

         return thrownException;
      }
   }
}
