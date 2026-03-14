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
      string? _sharedField;

      [XF] public void returns_the_field_value_without_calling_createValue_when_field_is_non_null()
      {
         var factoryCalled = false;
         string? field = "cached";
         var result = _monitor.DoubleCheckedLocking(ref field, () => { factoryCalled = true; return "new"; });

         result.Must().Be("cached");
         factoryCalled.Must().BeFalse();
      }

      [XF] public void creates_the_value_and_exchanges_into_field_when_field_is_null()
      {
         string? field = null;
         var result = _monitor.DoubleCheckedLocking(ref field, () => "populated");

         result.Must().Be("populated");
         field!.Must().Be("populated");
      }

      [XF] public void returns_the_value_from_tryRead_without_calling_createUpdatedFieldValue_when_tryRead_returns_non_null()
      {
         var factoryCalled = false;
         var dict = new Dictionary<string, string> { ["key"] = "value" };
         IReadOnlyDictionary<string, string> field = dict;
         var result = _monitor.DoubleCheckedLocking(
            tryRead: () => field.GetValueOrDefault("key"),
            field: ref field,
            createUpdatedFieldValue: () => { factoryCalled = true; return field; });

         result.Must().Be("value");
         factoryCalled.Must().BeFalse();
      }

      [XF] public void exchanges_the_field_and_returns_the_tryRead_value_when_tryRead_initially_returns_null()
      {
         IReadOnlyDictionary<string, string> field = new Dictionary<string, string>();
         var result = _monitor.DoubleCheckedLocking(
            tryRead: () => field.GetValueOrDefault("key"),
            field: ref field,
            createUpdatedFieldValue: () => new Dictionary<string, string>(field) { ["key"] = "populated" });

         result.Must().Be("populated");
      }

      [XF] public void throws_when_tryRead_returns_null_even_after_field_exchange()
      {
         string? field = null;
         Invoking(() => _monitor.DoubleCheckedLocking<string, string>(
            tryRead: () => null,
            field: ref field!,
            createUpdatedFieldValue: () => "populated"))
           .Must().Throw<Exception>();
      }

      [XF] public void concurrent_callers_all_get_the_same_result_and_createValue_runs_exactly_once()
      {
         _sharedField = null;
         var createCount = 0;
         string? resultA = null;
         string? resultB = null;
         var waitingToStart = IThreadGate.NewClosed(WaitTimeout.Seconds(5));
         var insideCreateValue = IThreadGate.NewClosed(WaitTimeout.Seconds(5));

         var runner = TestingTaskRunner.WithTimeout(10.Seconds());

         runner.Run(
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultA = _monitor.DoubleCheckedLocking(ref _sharedField, () =>
               {
                  insideCreateValue.AwaitPassThrough();
                  Interlocked.Increment(ref createCount);
                  return "populated";
               });
            },
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultB = _monitor.DoubleCheckedLocking(ref _sharedField, () =>
               {
                  insideCreateValue.AwaitPassThrough();
                  Interlocked.Increment(ref createCount);
                  return "populated";
               });
            });

         waitingToStart.AwaitQueueLengthEqualTo(2);
         waitingToStart.Open();
         insideCreateValue.AwaitQueueLengthEqualTo(1);
         insideCreateValue.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");
         insideCreateValue.Open();
         insideCreateValue.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");

         runner.Dispose();

         createCount.Must().Be(1);
         resultA!.Must().Be("populated");
         resultB!.Must().Be("populated");
      }
   }

   public class On_IAwaitableMonitor : DoubleCheckedLocking_specification
   {
      readonly IAwaitableMonitor _awaitableMonitor = IAwaitableMonitor.New();
      string? _sharedField;

      [XF] public void returns_the_field_value_without_calling_createValue_when_field_is_non_null()
      {
         var factoryCalled = false;
         string? field = "cached";
         var result = _awaitableMonitor.DoubleCheckedLocking(ref field, () => { factoryCalled = true; return "new"; });

         result.Must().Be("cached");
         factoryCalled.Must().BeFalse();
      }

      [XF] public void creates_the_value_and_exchanges_into_field_when_field_is_null()
      {
         string? field = null;
         var result = _awaitableMonitor.DoubleCheckedLocking(ref field, () => "populated");

         result.Must().Be("populated");
         field!.Must().Be("populated");
      }

      [XF] public void returns_the_value_from_tryRead_without_calling_createUpdatedFieldValue_when_tryRead_returns_non_null()
      {
         var factoryCalled = false;
         var dict = new Dictionary<string, string> { ["key"] = "value" };
         IReadOnlyDictionary<string, string> field = dict;
         var result = _awaitableMonitor.DoubleCheckedLocking(
            tryRead: () => field.GetValueOrDefault("key"),
            field: ref field,
            createUpdatedFieldValue: () => { factoryCalled = true; return field; });

         result.Must().Be("value");
         factoryCalled.Must().BeFalse();
      }

      [XF] public void exchanges_the_field_and_returns_the_tryRead_value_when_tryRead_initially_returns_null()
      {
         IReadOnlyDictionary<string, string> field = new Dictionary<string, string>();
         var result = _awaitableMonitor.DoubleCheckedLocking(
            tryRead: () => field.GetValueOrDefault("key"),
            field: ref field,
            createUpdatedFieldValue: () => new Dictionary<string, string>(field) { ["key"] = "populated" });

         result.Must().Be("populated");
      }

      [XF] public void throws_when_tryRead_returns_null_even_after_field_exchange()
      {
         string? field = null;
         Invoking(() => _awaitableMonitor.DoubleCheckedLocking<string, string>(
            tryRead: () => null,
            field: ref field!,
            createUpdatedFieldValue: () => "populated"))
           .Must().Throw<Exception>();
      }

      [XF] public void concurrent_callers_all_get_the_same_result_and_createValue_runs_exactly_once()
      {
         _sharedField = null;
         var createCount = 0;
         string? resultA = null;
         string? resultB = null;
         var waitingToStart = IThreadGate.NewClosed(WaitTimeout.Seconds(5));
         var insideCreateValue = IThreadGate.NewClosed(WaitTimeout.Seconds(5));

         var runner = TestingTaskRunner.WithTimeout(10.Seconds());

         runner.Run(
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultA = _awaitableMonitor.DoubleCheckedLocking(ref _sharedField, () =>
               {
                  insideCreateValue.AwaitPassThrough();
                  Interlocked.Increment(ref createCount);
                  return "populated";
               });
            },
            () =>
            {
               waitingToStart.AwaitPassThrough();
               resultB = _awaitableMonitor.DoubleCheckedLocking(ref _sharedField, () =>
               {
                  insideCreateValue.AwaitPassThrough();
                  Interlocked.Increment(ref createCount);
                  return "populated";
               });
            });

         waitingToStart.AwaitQueueLengthEqualTo(2);
         waitingToStart.Open();
         insideCreateValue.AwaitQueueLengthEqualTo(1);
         insideCreateValue.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");
         insideCreateValue.Open();
         insideCreateValue.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse("The second thread should not get here.");

         runner.Dispose();

         createCount.Must().Be(1);
         resultA!.Must().Be("populated");
         resultB!.Must().Be("populated");
      }
   }
}
