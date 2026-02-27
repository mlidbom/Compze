using System;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using Xunit;
using static Compze.Utilities.Testing.Must.MustActions;

// ReSharper disable InconsistentNaming
// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Unit.Internals.Testing.Threading;

[Collection(nameof(NonParallelCollection))]
public class Given_a_locked_ThreadGate : UniversalTestBase
{
   [XF] public void Calling_AllowOneThreadToPassThrough_throws_an_AwaitingConditionTimedOutException_since_no_threads_are_waiting_to_pass()
      => Invoking(() => ThreadGate.Closed(WaitTimeout.Milliseconds(20)).AwaitLetOneThreadPassThrough()).Must().Throw<AwaitingConditionTimeoutException>();

   public class After_starting_10_threads_that_all_call_PassThrough : UniversalTestBase
   {
      [XF] public void Within_10_seconds_all_threads_are_blocked_on_Passthrough_and_none_have_passed_the_gate()
      {
         using(ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough()) {} //warmup

         using var fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10);
         fixture.Gate.AwaitQueueLengthEqualTo(fixture.NumberOfThreads, WaitTimeout.Seconds(10));
         fixture.ThreadsPassedTheGate(0.Milliseconds()).Must().Be(0);
      }

      public sealed class And_all_have_queued_up_calling_PassThrough : UniversalTestBase, IDisposable
      {
         readonly ThreadGateTestFixture _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();

         protected override void DisposeInternal() => _fixture.Dispose();

         [XF] public void _10_milliseconds_later_no_thread_has_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Must().Be(0);
         [XF] public void PassedThrough_is_0() => _fixture.Gate.Passed.Must().Be(0);
         [XF] public void QueueLength_is_10() => _fixture.Gate.Queued.Must().Be(10);
         [XF] public void RequestCount_is_10() => _fixture.Gate.Requested.Must().Be(10);
      }
   }

   public sealed class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_once : UniversalTestBase, IDisposable
   {
      readonly ThreadGateTestFixture _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();

      public After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_once() =>
         _fixture.Gate.AwaitLetOneThreadPassThrough();

      protected override void DisposeInternal() => _fixture.Dispose();

      [XF] public void _10_milliseconds_later_one_thread_has_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Must().Be(1);
      [XF] public void PassedThrough_is_1() => _fixture.Gate.Passed.Must().Be(1);
      [XF] public void QueueLength_is_9() => _fixture.Gate.Queued.Must().Be(9);
      [XF] public void RequestCount_is_10() => _fixture.Gate.Requested.Must().Be(10);
   }

   public sealed class After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_five_times : UniversalTestBase, IDisposable
   {
      readonly ThreadGateTestFixture _fixture = ThreadGateTestFixture.StartEntrantsOnThreads(10).WaitForAllThreadsToQueueUpAtPassThrough();

      public After_10_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_five_times() =>
         1.Through(5).ForEach(_ => _fixture.Gate.AwaitLetOneThreadPassThrough());

      protected override void DisposeInternal() => _fixture.Dispose();

      [XF] public void _10_milliseconds_later_five_threads_have_passed_the_gate() => _fixture.ThreadsPassedTheGate(10.Milliseconds()).Must().Be(5);
      [XF] public void PassedThrough_is_5() => _fixture.Gate.Passed.Must().Be(5);
      [XF] public void QueueLength_is_5() => _fixture.Gate.Queued.Must().Be(5);
      [XF] public void RequestCount_is_10() => _fixture.Gate.Requested.Must().Be(10);
   }

   public class After_Y_threads_have_queued_up_at_PassThrough_and_LetOneThreadPassThrough_is_called_X_times_where_X_is_at_most_Y : UniversalTestBase
   {
      public static TheoryData<int, int> ThreadPassThroughTestData => new()
                                                                      {
                                                                         { 3, 1 },
                                                                         { 7, 2 },
                                                                         { 12, 3 },
                                                                         { 10, 4 },
                                                                         { 5, 5 },
                                                                         { 10, 6 },
                                                                         { 10, 7 },
                                                                         { 10, 8 },
                                                                         { 10, 9 },
                                                                         { 10, 10 }
                                                                      };

      static void RunTest(int threads, int timesToCallLetOneThreadPassThrough, Action<ThreadGateTestFixture> test)
      {
         using var fixture = ThreadGateTestFixture.StartEntrantsOnThreads(threads).WaitForAllThreadsToQueueUpAtPassThrough();
         1.Through(timesToCallLetOneThreadPassThrough).ForEach(_ => fixture.Gate.AwaitLetOneThreadPassThrough());
         test(fixture);
      }

      [Theory, MemberData(nameof(ThreadPassThroughTestData))]
      public void _200_milliseconds_later_X_threads_have_passed_the_gate(int threads, int timesToCallLetOneThreadPassThrough) =>
         RunTest(threads, timesToCallLetOneThreadPassThrough, fixture => fixture.ThreadsPassedTheGate(200.Milliseconds()).Must().Be(timesToCallLetOneThreadPassThrough));

      [Theory, MemberData(nameof(ThreadPassThroughTestData))]
      public void PassedThrough_is_X(int threads, int timesToCallLetOneThreadPassThrough) =>
         RunTest(threads, timesToCallLetOneThreadPassThrough, fixture => fixture.Gate.Passed.Must().Be(timesToCallLetOneThreadPassThrough));

      [Theory, MemberData(nameof(ThreadPassThroughTestData))]
      public void QueueLength_is_Y_minus_X(int threads, int timesToCallLetOneThreadPassThrough) =>
         RunTest(threads, timesToCallLetOneThreadPassThrough, fixture => fixture.Gate.Queued.Must().Be(Math.Max(0, threads - timesToCallLetOneThreadPassThrough)));

      [Theory, MemberData(nameof(ThreadPassThroughTestData))]
      public void RequestCount_is_Y(int threads, int timesToCallLetOneThreadPassThrough) =>
         RunTest(threads, timesToCallLetOneThreadPassThrough, fixture => fixture.Gate.Requested.Must().Be(threads));
   }
}
