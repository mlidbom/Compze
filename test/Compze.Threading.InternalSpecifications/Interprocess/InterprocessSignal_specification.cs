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
         _signal.TryAwait(TimeSpan.FromMilliseconds(100)).Must().BeTrue();
      }

      [XF] public void returns_false_when_no_signal_is_raised_within_timeout() => _signal.TryAwait(TimeSpan.FromMilliseconds(50)).Must().BeFalse();

      [XF] public void does_not_return_until_signal_is_raised()
      {
         var beforeAwaitingGate = IThreadGate.NewOpen(WaitTimeout.Seconds(5));
         var afterAwaitingGate = IThreadGate.NewOpen(WaitTimeout.Seconds(5));

         _runner.Run(() =>
         {
            beforeAwaitingGate.AwaitPassThrough();
            // ReSharper disable once AccessToDisposedClosure
            _signal.TryAwait(TimeSpan.FromSeconds(2));
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
         _signal.TryAwait(TimeSpan.FromMilliseconds(100)).Must().BeTrue();

         // No new Raise — should timeout
         _signal.TryAwait(TimeSpan.FromMilliseconds(50)).Must().BeFalse();
      }
   }

   public class Snapshot : InterprocessSignal_specification
   {
      [XF] public void resets_baseline_so_previous_raise_is_not_detected()
      {
         _signal.Raise();
         _signal.Snapshot();

         // The raise happened before the snapshot — should not be detected
         _signal.TryAwait(TimeSpan.FromMilliseconds(50)).Must().BeFalse();
      }

      [XF] public void allows_detecting_raise_after_snapshot()
      {
         _signal.Raise();
         _signal.Snapshot();
         _signal.Raise();

         _signal.TryAwait(TimeSpan.FromMilliseconds(100)).Must().BeTrue();
      }
   }

   public class Two_instances_with_the_same_name : InterprocessSignal_specification
   {
      [XF] public void one_raises_the_other_detects()
      {
         const string name = "InterprocessSignal_specification.TwoInstances.cross_detect";
         using var raiser = new InterprocessSignal(name, TestDirectory);
         using var waiter = new InterprocessSignal(name, TestDirectory);

         raiser.Raise();
         waiter.TryAwait(TimeSpan.FromMilliseconds(100)).Must().BeTrue();
      }
   }
}
