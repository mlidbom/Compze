using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

// ReSharper disable AccessToDisposedClosure

namespace Composable.Tests.SystemCE.ThreadingCE.ResourceAccess;

[TestFixture] public class MonitorCE_specification
{
   [Test] public void When_one_thread_has_UpdateLock_other_thread_is_blocked_until_first_thread_disposes_lock_()
   {
      var monitor = MonitorCE.WithTimeout(1.Seconds());

      var updateLock = monitor.EnterUpdateLock();

      using var otherThreadIsWaitingForLock = new ManualResetEventSlim(false);
      using var otherThreadGotLock = new ManualResetEventSlim(false);
      var otherThreadTask = TaskCE.Run(() =>
      {
         otherThreadIsWaitingForLock.Set();
         using(monitor.EnterUpdateLock())
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

   [Test] public void When_one_thread_calls_AwaitUpdateLock_twice_an_exception_is_thrown()
   {
      var monitor = MonitorCE.WithTimeout(1.Seconds());

      using(monitor.EnterUpdateLock())
      {
         Assert.Throws<InvalidOperationException>(() => monitor.EnterUpdateLock());
      }

   }

   [TestFixture] public class An_exception_is_thrown_by_EnterUpdateLock_if_lock_is_not_acquired_within_timeout
   {
      [NCrunch.Framework.RestrictToString(false)]
      [Test] public void Exception_is_ObjectLockTimedOutException() =>
         RunScenario(ownerThreadBlockTime:20.Milliseconds(), monitorTimeout: 10.Milliseconds()).Should().BeOfType<EnterLockTimeoutException>();

      [Test,EnableRdi(false)] public void If_owner_thread_blocks_for_less_than_fetchStackTraceTimeout_Exception_contains_owning_threads_stack_trace() =>
         RunScenario(ownerThreadBlockTime: 20.Milliseconds(), 5.Milliseconds()).Message.Should().Contain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      [Test] public void If_owner_thread_blocks_for_more_than_fetchStackTraceTimeout_Exception_does_not_contain_owning_threads_stack_trace() =>
          RunScenario(ownerThreadBlockTime: 60.Milliseconds(), timeToWaitForStackTrace: 1.Milliseconds(), monitorTimeout:5.Milliseconds()).Message.Should().NotContain(nameof(DisposeInMethodSoItWillBeInTheCapturedCallStack));

      internal static void DisposeInMethodSoItWillBeInTheCapturedCallStack(IDisposable disposable) => disposable.Dispose();

      static Exception RunScenario(TimeSpan ownerThreadBlockTime, TimeSpan monitorTimeout, TimeSpan? timeToWaitForStackTrace = null)
      {
         var monitor = MonitorCE.WithTimeout(monitorTimeout);
         if (timeToWaitForStackTrace.HasValue)
         {
            monitor.SetTimeToWaitForStackTrac(timeToWaitForStackTrace.Value);
         }

         var threadOneHasTakenUpdateLock = new ManualResetEvent(false);
         var threadTwoIsAboutToTryToEnterUpdateLock = new ManualResetEvent(false);

         TaskCE.Run(() =>
         {
            var @lock = monitor.EnterUpdateLock();
            threadOneHasTakenUpdateLock.Set();
            threadTwoIsAboutToTryToEnterUpdateLock.WaitOne();
            Thread.Sleep(ownerThreadBlockTime);
            DisposeInMethodSoItWillBeInTheCapturedCallStack(@lock);
         });

         threadOneHasTakenUpdateLock.WaitOne();

         var thrownException = Assert.Throws<AggregateException>(
                                         () => TaskCE.Run(() =>
                                                      {
                                                         threadTwoIsAboutToTryToEnterUpdateLock.Set();
                                                         monitor.EnterUpdateLock();
                                                      })
                                                     .Wait())
                                     .InnerExceptions.Single();

         return thrownException;
      }

      static void RunWithChangedFetchStackTraceTimeout(TimeSpan fetchStackTraceTimeout, Action action)
      {
         var timeoutProperty = typeof(EnterLockTimeoutException).GetField("_timeToWaitForOwningThreadStacktrace", BindingFlags.Static | BindingFlags.NonPublic)!;
         var original = timeoutProperty.GetValue(null);
         using(DisposableCE.Create(() => timeoutProperty.SetValue(null, original)))
         {
            timeoutProperty.SetValue(null, fetchStackTraceTimeout);
            action();
         }
      }
   }
}