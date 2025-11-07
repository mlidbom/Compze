using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using NCrunch.Framework;
using Xunit;
using static Compze.Utilities.Testing.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE.ResourceAccess;

[Collection(nameof(NonParallelCollection))]
public class MonitorCE_specification : UniversalTestBase
{
   [XF] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
   {
      var monitor = IMonitorCE.WithTimeouts(1.Seconds());

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
      var monitor = IMonitorCE.WithTimeouts(1.Seconds());
      using(monitor.TakeUpdateLock())
      {
         using(monitor.TakeUpdateLock()) {}

         Invoking(() => TaskCE.Run(() => monitor.TakeUpdateLock(timeout: 100.Milliseconds())).Wait())
           .Must().Throw<Exception>();
      }

      TaskCE.Run(() => monitor.TakeUpdateLock(timeout: 0.Milliseconds())).Wait();
   }

   public class An_exception_is_thrown_by_EnterUpdateLock_if_lock_is_not_acquired_within_timeout : UniversalTestBase
   {
      [XF, EnableRdi(false)] public void Exception_is_ObjectLockTimedOutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), timeToWaitForStackTrace: 30.Seconds(), monitorTimeout: 10.Milliseconds()).Must().BeOfType<TakeLockTimeoutException>();

      [XF, EnableRdi(false)] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), timeToWaitForStackTrace: 30.Seconds(), monitorTimeout: 5.Milliseconds()).Message.Must().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [XF] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), timeToWaitForStackTrace: 1.Milliseconds(), monitorTimeout: 5.Milliseconds()).Message.Must().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      static Exception RunScenario(TimeSpan ownerThreadBlockTime, TimeSpan monitorTimeout, TimeSpan? timeToWaitForStackTrace = null)
      {
         var monitor = IMonitorCE.WithTimeouts(monitorTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
            monitor.SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
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
