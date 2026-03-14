using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming
// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications;

[Collection(nameof(NonParallelCollection))]
public class DoubleCheckedLocking_specification : UniversalTestBase
{
   public class On_ICriticalSection : DoubleCheckedLocking_specification
   {
      readonly ICriticalSection _monitor = IMonitor.New();

      [XF] public void returns_the_value_from_tryRead_without_calling_updateOnFailedRead_when_tryRead_returns_non_null()
      {
         var updateCalled = false;
         var result = _monitor.DoubleCheckedLocking(
            tryRead: () => "cached",
            updateOnFailedRead: () => updateCalled = true);

         result.Must().Be("cached");
         updateCalled.Must().BeFalse();
      }

      [XF] public void calls_updateOnFailedRead_and_returns_the_value_when_tryRead_initially_returns_null()
      {
         string? value = null;
         var result = _monitor.DoubleCheckedLocking(
            tryRead: () => value,
            updateOnFailedRead: () => value = "populated");

         result.Must().Be("populated");
      }

      [XF] public void throws_when_tryRead_returns_null_even_after_updateOnFailedRead()
      {
         Invoking(() => _monitor.DoubleCheckedLocking<string>(
            tryRead: () => null,
            updateOnFailedRead: () => { }))
           .Must().Throw<Exception>();
      }

      [XF] public void concurrent_callers_all_get_the_same_result_and_updateOnFailedRead_runs_exactly_once()
      {
         var updateCount = 0;
         string? value = null;
         string? resultA = null;
         string? resultB = null;
         var waitingToStart = IThreadGate.NewClosed(WaitTimeout.Seconds(5));
         var insideUpdateOnFailedRead = IThreadGate.NewClosed(WaitTimeout.Seconds(5));

         var runner = TestingTaskRunner.WithTimeout(10.Seconds());

         runner.Run(
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultA = _monitor.DoubleCheckedLocking(
                  tryRead: () => value,
                  updateOnFailedRead: () =>
                  {
                     insideUpdateOnFailedRead.AwaitPassThrough();
                     Interlocked.Increment(ref updateCount);
                     value = "populated";
                  });
            },
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultB = _monitor.DoubleCheckedLocking(
                  tryRead: () => value,
                  updateOnFailedRead: () =>
                  {
                     insideUpdateOnFailedRead.AwaitPassThrough();
                     Interlocked.Increment(ref updateCount);
                     value = "populated";
                  });
            });

         waitingToStart.AwaitQueueLengthEqualTo(2);
         waitingToStart.Open();
         insideUpdateOnFailedRead.AwaitQueueLengthEqualTo(1);
         insideUpdateOnFailedRead.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");
         insideUpdateOnFailedRead.Open();
         insideUpdateOnFailedRead.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");

         runner.Dispose();

         updateCount.Must().Be(1);
         resultA!.Must().Be("populated");
         resultB!.Must().Be("populated");
      }
   }

   public class On_IAwaitableMonitor : DoubleCheckedLocking_specification
   {
      readonly IAwaitableMonitor _awaitableMonitor = IAwaitableMonitor.New();

      [XF] public void returns_the_value_from_tryRead_without_calling_updateOnFailedRead_when_tryRead_returns_non_null()
      {
         var updateCalled = false;
         var result = _awaitableMonitor.DoubleCheckedLocking(
            tryRead: () => "cached",
            updateOnFailedRead: () => updateCalled = true);

         result.Must().Be("cached");
         updateCalled.Must().BeFalse();
      }

      [XF] public void calls_updateOnFailedRead_and_returns_the_value_when_tryRead_initially_returns_null()
      {
         string? value = null;
         var result = _awaitableMonitor.DoubleCheckedLocking(
            tryRead: () => value,
            updateOnFailedRead: () => value = "populated");

         result.Must().Be("populated");
      }

      [XF] public void throws_when_tryRead_returns_null_even_after_updateOnFailedRead()
      {
         Invoking(() => _awaitableMonitor.DoubleCheckedLocking<string>(
            tryRead: () => null,
            updateOnFailedRead: () => { }))
           .Must().Throw<Exception>();
      }

      [XF] public void concurrent_callers_all_get_the_same_result_and_updateOnFailedRead_runs_exactly_once()
      {
         var updateCount = 0;
         string? value = null;
         string? resultA = null;
         string? resultB = null;
         var waitingToStart = IThreadGate.NewClosed(WaitTimeout.Seconds(5));
         var insideUpdateOnFailedRead = IThreadGate.NewClosed(WaitTimeout.Seconds(5));

         var runner = TestingTaskRunner.WithTimeout(10.Seconds());

         runner.Run(
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultA = _awaitableMonitor.DoubleCheckedLocking(
                  tryRead: () => value,
                  updateOnFailedRead: () =>
                  {
                     insideUpdateOnFailedRead.AwaitPassThrough();
                     Interlocked.Increment(ref updateCount);
                     value = "populated";
                  });
            },
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultB = _awaitableMonitor.DoubleCheckedLocking(
                  tryRead: () => value,
                  updateOnFailedRead: () =>
                  {
                     insideUpdateOnFailedRead.AwaitPassThrough();
                     Interlocked.Increment(ref updateCount);
                     value = "populated";
                  });
            });

         waitingToStart.AwaitQueueLengthEqualTo(2);
         waitingToStart.Open();
         insideUpdateOnFailedRead.AwaitQueueLengthEqualTo(1);
         insideUpdateOnFailedRead.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");
         insideUpdateOnFailedRead.Open();
         insideUpdateOnFailedRead.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");

         runner.Dispose();

         updateCount.Must().Be(1);
         resultA!.Must().Be("populated");
         resultB!.Must().Be("populated");
      }
   }
}
