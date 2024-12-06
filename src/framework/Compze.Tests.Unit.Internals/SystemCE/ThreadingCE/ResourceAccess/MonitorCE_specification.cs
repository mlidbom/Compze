using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using NCrunch.Framework;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.SystemCE.ThreadingCE.ResourceAccess;

[TestFixture] public class MonitorCE_specification : UniversalTestBase
{
   [Test] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
   {
      var monitor = MonitorCE.WithTimeout(1.Seconds());

      var updateLock = monitor.TakeUpdateLock();

      using var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
      using var otherThreadGotLock = new ManualResetEventSlim(false);
      var otherThreadTask = Task.Run(() =>
      {
         otherThreadIsWaitingForLock.Set();
         using(monitor.TakeUpdateLock())
         {
            otherThreadGotLock.Set();
         }
      });

      otherThreadIsWaitingForLock.Wait();
      otherThreadGotLock.Wait(10.Milliseconds()).Should().BeFalse();

      updateLock.Dispose();
      otherThreadGotLock.Wait(10.Milliseconds()).Should().BeTrue();

      Task.WaitAll(otherThreadTask);
   }

   [Test] public void Owning_thread_can_reenter_the_lock_and_the_lock_is_only_exited_when_releasing_the_outermost_lock()
   {
      var monitor = MonitorCE.WithTimeout(1.Seconds());

      using(monitor.TakeUpdateLock())
      {
         using(monitor.TakeUpdateLock()) {}

         FluentActions.Invoking(() => Task.Run(() => monitor.TakeUpdateLock(timeout: 10.Milliseconds())).Wait())
                                       .Should().Throw<Exception>();
      }

      Task.Run(() => monitor.TakeUpdateLock(timeout: 0.Milliseconds())).Wait();
   }

   [TestFixture] public class An_exception_is_thrown_by_EnterUpdateLock_if_lock_is_not_acquired_within_timeout : UniversalTestBase
   {
      [Test, EnableRdi(false)] public void Exception_is_ObjectLockTimedOutException() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), timeToWaitForStackTrace: 5.Seconds(), monitorTimeout: 10.Milliseconds()).Should().BeOfType<EnterLockTimeoutException>();

      [Test, EnableRdi(false)] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), timeToWaitForStackTrace: 5.Seconds(), monitorTimeout: 5.Milliseconds()).Message.Should().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [Test] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 60.Milliseconds(), timeToWaitForStackTrace: 1.Milliseconds(), monitorTimeout: 5.Milliseconds()).Message.Should().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      static Exception RunScenario(TimeSpan ownerThreadBlockTime, TimeSpan monitorTimeout, TimeSpan? timeToWaitForStackTrace = null)
      {
         var monitor = MonitorCE.WithTimeout(monitorTimeout);
         if(timeToWaitForStackTrace.HasValue)
         {
            monitor.SetTimeToWaitForStackTrace(timeToWaitForStackTrace.Value);
         }

         var threadOneHasTakenUpdateLock = new ManualResetEvent(false);
         var threadTwoIsAboutToTryToEnterUpdateLock = new ManualResetEvent(false);

         Task.Run(() =>
         {
            var @lock = monitor.TakeUpdateLock();
            threadOneHasTakenUpdateLock.Set();
            threadTwoIsAboutToTryToEnterUpdateLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(@lock);
         });

         threadOneHasTakenUpdateLock.WaitOne();

         var thrownException = Assert.Throws<AggregateException>(
                                         () => Task.Run(() =>
                                                    {
                                                       threadTwoIsAboutToTryToEnterUpdateLock.Set();
                                                       monitor.TakeUpdateLock();
                                                    })
                                                   .Wait())
                                     .InnerExceptions.Single();

         return thrownException;
      }
   }
}
