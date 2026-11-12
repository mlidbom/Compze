using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Testing.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class Async_waits_on_a_ThreadGate : UniversalTestBase
{
   static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);
   static readonly WaitTimeout AShortWaitTimeout = WaitTimeout.Milliseconds(100);

   readonly IThreadGate _gate = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "gate");

   public class AwaitPassedThroughCountEqualToAsync : Async_waits_on_a_ThreadGate
   {
      public class when_a_pass_through_reaches_the_count : AwaitPassedThroughCountEqualToAsync
      {
         readonly Task _wait;

         public when_a_pass_through_reaches_the_count()
         {
            _wait = _gate.AwaitPassedThroughCountEqualToAsync(1);
            _gate.AwaitPassThrough();
         }

         [XF] public async Task the_wait_completes() => (await _wait.CompletesWithinAsync(Patience)).Must().BeTrue();
      }

      public class when_the_count_is_not_reached_before_the_timeout_expires : AwaitPassedThroughCountEqualToAsync
      {
         readonly Task _wait;
         public when_the_count_is_not_reached_before_the_timeout_expires() => _wait = _gate.AwaitPassedThroughCountEqualToAsync(1, AShortWaitTimeout);

         [XF] public async Task it_fails_with_an_AwaitingConditionTimeoutException_naming_the_condition_the_gates_state_and_the_thread_pools_state() =>
            (await InvokingAsync(() => _wait).Must().ThrowAsync<AwaitingConditionTimeoutException>())
           .Which.Message.Must().Contain("Passed == 1").Contain("Current state of gate").Contain("Thread pool");
      }
   }

   public class TryAwaitPassedThroughCountEqualToAsync : Async_waits_on_a_ThreadGate
   {
      public class when_the_count_is_not_reached_before_the_timeout_expires : TryAwaitPassedThroughCountEqualToAsync
      {
         readonly Task<bool> _wait;
         public when_the_count_is_not_reached_before_the_timeout_expires() => _wait = _gate.TryAwaitPassedThroughCountEqualToAsync(1, AShortWaitTimeout);

         [XF] public async Task it_returns_false() => (await _wait.WaitAsync(Patience)).Must().BeFalse();
      }
   }

   public class TryAwaitQueueLengthEqualToAsync : Async_waits_on_a_ThreadGate
   {
      public class when_an_async_pass_queues_at_the_closed_gate : TryAwaitQueueLengthEqualToAsync
      {
         readonly Task _pass;
         readonly Task<bool> _wait;

         public when_an_async_pass_queues_at_the_closed_gate()
         {
            _gate.Close();
            _wait = _gate.TryAwaitQueueLengthEqualToAsync(1);
            _pass = _gate.AwaitPassThroughAsync();
         }

         protected override async Task DisposeAsyncInternal()
         {
            _gate.Open();
            await _pass.AwaitCompletionWithinAsync(Patience);
            await base.DisposeAsyncInternal();
         }

         [XF] public async Task it_returns_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
      }

      public class when_nothing_queues_before_the_timeout_expires : TryAwaitQueueLengthEqualToAsync
      {
         readonly Task<bool> _wait;
         public when_nothing_queues_before_the_timeout_expires() => _wait = _gate.TryAwaitQueueLengthEqualToAsync(1, AShortWaitTimeout);

         [XF] public async Task it_returns_false() => (await _wait.WaitAsync(Patience)).Must().BeFalse();
      }
   }

   public class AwaitClosedAsync : Async_waits_on_a_ThreadGate
   {
      public class when_the_gate_closes : AwaitClosedAsync
      {
         readonly Task _wait;

         public when_the_gate_closes()
         {
            _wait = _gate.AwaitClosedAsync();
            _gate.Close();
         }

         [XF] public async Task the_wait_completes() => (await _wait.CompletesWithinAsync(Patience)).Must().BeTrue();
      }
   }
}
