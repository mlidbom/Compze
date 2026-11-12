using System.Diagnostics;
using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Testing.Specifications.Async;

[Collection(nameof(NonParallelCollection))]
public class An_async_pass_through_a_closed_ThreadGate : UniversalTestBase
{
   static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);
   static readonly TimeSpan TimeAWokenPassWouldHaveCompletedIn = TimeSpan.FromMilliseconds(100);

   readonly IThreadGate _gate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "gate");
   readonly Task _pass;

   public An_async_pass_through_a_closed_ThreadGate() => _pass = _gate.AwaitPassThroughAsync();

   protected override async Task DisposeAsyncInternal()
   {
      _gate.Open();
      await _pass.AwaitCompletionWithinAsync(Patience);
      await base.DisposeAsyncInternal();
   }

   [XF] public async Task does_not_complete() => (await _pass.CompletesWithinAsync(TimeAWokenPassWouldHaveCompletedIn)).Must().BeFalse();
   [XF] public void is_queued_at_the_gate() => _gate.Queued.Must().Be(1);
   [XF] public void is_counted_as_requested() => _gate.Requested.Must().Be(1);

   public class after_the_gate_opens : An_async_pass_through_a_closed_ThreadGate
   {
      public after_the_gate_opens() => _gate.Open();

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _pass.WaitAsync(Patience);
      }

      [XF] public void it_has_passed_through() => _gate.Passed.Must().Be(1);
      [XF] public void it_is_no_longer_queued() => _gate.Queued.Must().Be(0);
   }

   public class and_another_one_after_AwaitLetOneThreadPassThroughAsync : An_async_pass_through_a_closed_ThreadGate
   {
      readonly Task _secondPass;
      ThreadSnapshot _snapshotOfTheOneLetThrough = null!;

      public and_another_one_after_AwaitLetOneThreadPassThroughAsync() => _secondPass = _gate.AwaitPassThroughAsync();

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _snapshotOfTheOneLetThrough = await _gate.AwaitLetOneThreadPassThroughAsync().WaitAsync(Patience);
      }

      protected override async Task DisposeAsyncInternal()
      {
         _gate.Open();
         await _secondPass.AwaitCompletionWithinAsync(Patience);
         await base.DisposeAsyncInternal();
      }

      [XF] public void exactly_one_has_passed_through() => _gate.Passed.Must().Be(1);
      [XF] public void the_other_is_still_queued() => _gate.Queued.Must().Be(1);
      [XF] public void the_gate_is_closed_again() => _gate.IsOpen.Must().BeFalse();
      [XF] public void it_returns_the_snapshot_of_the_one_it_let_through() => _snapshotOfTheOneLetThrough.Must().ReferenceEqual(_gate.PassedThrough[0]);
   }

   public class and_a_synchronous_pass_on_another_thread : An_async_pass_through_a_closed_ThreadGate
   {
      readonly Task _synchronousPass;

      public and_a_synchronous_pass_on_another_thread() => _synchronousPass = TaskCE.RunOnDedicatedThread(() => _gate.AwaitPassThrough());

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _gate.AwaitQueueLengthEqualToAsync(2);
         _gate.Open();
         await Task.WhenAll(_pass, _synchronousPass).WaitAsync(Patience);
      }

      [XF] public void both_pass_through_when_the_gate_opens() => _gate.Passed.Must().Be(2);
   }

   public class when_the_post_pass_through_action_throws : An_async_pass_through_a_closed_ThreadGate
   {
      public when_the_post_pass_through_action_throws()
      {
         _gate.ThrowPostPassThrough(new IntentionalSpecificationException());
         _gate.Open();
      }

      [XF] public async Task the_pass_fails_with_that_exception() => await InvokingAsync(() => _pass).Must().ThrowAsync<IntentionalSpecificationException>();
   }
}
