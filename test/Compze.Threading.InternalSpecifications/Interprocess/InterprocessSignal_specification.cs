using System.Diagnostics;
using Compze.Internals.SystemCE;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.InternalSpecifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.Underscore;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.InternalSpecifications.Interprocess;

[Collection(nameof(NonParallelCollection))]
public class InterprocessSignal_specification : UniversalTestBase
{
   static readonly DirectoryInfo TestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "Signals"))._mutate(it => it.Create());

   readonly TestingTaskRunner _runner = new(10.Seconds());
   readonly InterprocessSignal _signal = new(Guid.NewGuid().ToString(), TestDirectory);
   long _baseline;

   public InterprocessSignal_specification() => _baseline = _signal.Snapshot();

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _signal.Dispose();
   }

   public class Construction : InterprocessSignal_specification
   {
      [XF] public void throws_ArgumentException_when_name_is_empty() =>
         Invoking(() => new InterprocessSignal("", TestDirectory)).Must().Throw<ArgumentException>();

      [XF] public void throws_ArgumentException_when_name_is_whitespace() =>
         Invoking(() => new InterprocessSignal("   ", TestDirectory)).Must().Throw<ArgumentException>();

      [XF] public void throws_DirectoryNotFoundException_when_directory_does_not_exist() =>
         Invoking(() => new InterprocessSignal("test", new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())))).Must().Throw<DirectoryNotFoundException>();
   }

   public class TryAwait : InterprocessSignal_specification
   {
      [XF] public void returns_true_immediately_when_signal_was_raised_before_call()
      {
         _signal.Raise();
         _signal.TryAwait(TimeSpan.FromMilliseconds(100), ref _baseline).Must().BeTrue();
      }

      [XF] public void returns_false_when_no_signal_is_raised_within_timeout() => _signal.TryAwait(TimeSpan.FromMilliseconds(50), ref _baseline).Must().BeFalse();

      [XF] public void does_not_return_until_signal_is_raised()
      {
         var beforeAwaitingGate = IThreadGate.NewOpen(WaitTimeout.Seconds(5));
         var afterAwaitingGate = IThreadGate.NewOpen(WaitTimeout.Seconds(5));
         var localBaseline = _baseline;

         _runner.Run(() =>
         {
            beforeAwaitingGate.AwaitPassThrough();
            // ReSharper disable once AccessToDisposedClosure
            _signal.TryAwait(TimeSpan.FromSeconds(2), ref localBaseline);
            afterAwaitingGate.AwaitPassThrough();
         });


         beforeAwaitingGate.TryAwaitPassedThroughCountEqualTo(1).Must().BeTrue("We did not start waiting timely");
         afterAwaitingGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse("We must not pass TryAwait before the signal is raised");
         _signal.Raise();
         afterAwaitingGate.TryAwaitPassedThroughCountEqualTo(1).Must().BeTrue("We were still blocked even after signal was raised.");
      }

      [XF] public void returns_false_after_consuming_a_signal_without_new_raise()
      {
         _signal.Raise();
         _signal.TryAwait(TimeSpan.FromMilliseconds(100), ref _baseline).Must().BeTrue();

         // No new Raise — should timeout
         _signal.TryAwait(TimeSpan.FromMilliseconds(50), ref _baseline).Must().BeFalse();
      }
   }

   public class Snapshot : InterprocessSignal_specification
   {
      [XF] public void resets_baseline_so_previous_raise_is_not_detected()
      {
         _signal.Raise();
         _baseline = _signal.Snapshot();

         // The raise happened before the snapshot — should not be detected
         _signal.TryAwait(TimeSpan.FromMilliseconds(50), ref _baseline).Must().BeFalse();
      }

      [XF] public void allows_detecting_raise_after_snapshot()
      {
         _signal.Raise();
         _baseline = _signal.Snapshot();
         _signal.Raise();

         _signal.TryAwait(TimeSpan.FromMilliseconds(100), ref _baseline).Must().BeTrue();
      }
   }

   public class With_a_custom_polling_policy : InterprocessSignal_specification
   {
      [XF] public void a_poll_interval_longer_than_the_timeout_does_not_delay_the_timeout()
      {
         using var slowPollingSignal = new InterprocessSignal(Guid.NewGuid().ToString(), TestDirectory, new FixedPollIntervalPolicy(TimeSpan.FromSeconds(10)));
         var baseline = slowPollingSignal.Snapshot();

         var stopwatch = Stopwatch.StartNew();
         slowPollingSignal.TryAwait(TimeSpan.FromMilliseconds(100), ref baseline).Must().BeFalse();
         (stopwatch.Elapsed < TimeSpan.FromSeconds(5)).Must().BeTrue("The poll interval must be clamped to the timeout deadline");
      }

      [XF] public void cancellation_interrupts_a_poll_sleep_mid_interval()
      {
         using var slowPollingSignal = new InterprocessSignal(Guid.NewGuid().ToString(), TestDirectory, new FixedPollIntervalPolicy(TimeSpan.FromSeconds(10)));
         var localBaseline = slowPollingSignal.Snapshot();
         using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

         var stopwatch = Stopwatch.StartNew();
         Invoking(() => slowPollingSignal.TryAwait(TimeSpan.FromSeconds(30), ref localBaseline, cancellation.Token)).Must().Throw<OperationCanceledException>();
         (stopwatch.Elapsed < TimeSpan.FromSeconds(5)).Must().BeTrue("Cancellation must wake the waiter instead of waiting out the 10 second poll interval");
      }

      [XF] public void elapsed_time_is_measured_from_waitStartedAt_so_backoff_spans_chunked_TryAwait_calls()
      {
         var recordingPolicy = new ElapsedWaitTimeRecordingPolicy();
         using var signal = new InterprocessSignal(Guid.NewGuid().ToString(), TestDirectory, recordingPolicy);
         var baseline = signal.Snapshot();

         signal.TryAwait(TimeSpan.FromMilliseconds(20), ref baseline, waitStartedAt: DateTime.UtcNow - TimeSpan.FromMinutes(10)).Must().BeFalse();
         (recordingPolicy.LargestElapsedWaitTimeSeen >= TimeSpan.FromMinutes(10)).Must().BeTrue("The policy must see the logical wait's full elapsed time, not the time since this TryAwait call");
      }

      class FixedPollIntervalPolicy : ISignalPollingPolicy
      {
         readonly TimeSpan _interval;
         public FixedPollIntervalPolicy(TimeSpan interval) => _interval = interval;
         public TimeSpan NextPollInterval(TimeSpan elapsedWaitTime) => _interval;
      }

      class ElapsedWaitTimeRecordingPolicy : ISignalPollingPolicy
      {
         internal TimeSpan LargestElapsedWaitTimeSeen;

         public TimeSpan NextPollInterval(TimeSpan elapsedWaitTime)
         {
            if(elapsedWaitTime > LargestElapsedWaitTimeSeen) LargestElapsedWaitTimeSeen = elapsedWaitTime;
            return TimeSpan.FromMilliseconds(1);
         }
      }
   }

   public class Two_instances_with_the_same_name : InterprocessSignal_specification
   {
      [XF] public void one_raises_the_other_detects()
      {
         const string name = "InterprocessSignal_specification.TwoInstances.cross_detect";
         using var raiser = new InterprocessSignal(name, TestDirectory);
         using var waiter = new InterprocessSignal(name, TestDirectory);

         var waiterBaseline = waiter.Snapshot();
         raiser.Raise();
         waiter.TryAwait(TimeSpan.FromMilliseconds(100), ref waiterBaseline).Must().BeTrue();
      }
   }
}
