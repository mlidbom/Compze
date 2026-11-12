using Compze.Tests.Infrastructure;
using Compze.Contracts.Exceptions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class TryUpdateWhenAsync : AsyncConditionWaitSpecification
{
   public class when_the_condition_already_holds : TryUpdateWhenAsync
   {
      bool _actionRan;
      readonly Task<bool> _wait;
      public when_the_condition_already_holds() => _wait = CriticalSection.TryUpdateWhenAsync(() => true, () => _actionRan = true);

      [IAwaitableCriticalSectionMatrix] public void the_action_has_run_by_the_time_the_call_returns() => _actionRan.Must().BeTrue();
      [IAwaitableCriticalSectionMatrix] public async Task the_wait_returns_true() => (await _wait).Must().BeTrue();
   }

   public class when_the_condition_does_not_hold : TryUpdateWhenAsync
   {
      bool _conditionHolds;
      int _actionRuns;
      bool _anotherThreadCouldTakeTheLockWhileTheActionRan;
      readonly Task<bool> _wait;

      public when_the_condition_does_not_hold() => _wait = CriticalSection.TryUpdateWhenAsync(() => _conditionHolds, () =>
      {
         _actionRuns++;
         _anotherThreadCouldTakeTheLockWhileTheActionRan = AnotherThreadCanTakeTheLock();
      });

      protected override async Task DisposeAsyncInternal()
      {
         CriticalSection.Update(() => _conditionHolds = true);
         await _wait.AwaitCompletionWithinAsync(Patience);
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_does_not_complete() => (await _wait.CompletesWithinAsync(TimeAWokenWaitWouldHaveCompletedIn)).Must().BeFalse();
      [IAwaitableCriticalSectionMatrix] public void the_action_has_not_run() => CriticalSection.Read(() => _actionRuns).Must().Be(0);

      public class and_an_update_makes_it_true : when_the_condition_does_not_hold
      {
         public and_an_update_makes_it_true() => CriticalSection.Update(() => _conditionHolds = true);

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await _wait.AwaitCompletionWithinAsync(Patience);
         }

         [IAwaitableCriticalSectionMatrix] public async Task the_wait_returns_true() => (await _wait).Must().BeTrue();
         [IAwaitableCriticalSectionMatrix] public void the_action_ran_once() => _actionRuns.Must().Be(1);
         [IAwaitableCriticalSectionMatrix] public void no_other_thread_could_take_the_lock_while_the_action_ran() => _anotherThreadCouldTakeTheLockWhileTheActionRan.Must().BeFalse();
      }
   }

   public class when_another_async_wait_waits_for_what_its_action_does : TryUpdateWhenAsync
   {
      bool _actionRan;
      readonly Task<bool> _otherWait;

      public when_another_async_wait_waits_for_what_its_action_does()
      {
         _otherWait = CriticalSection.TryAwaitAsync(() => _actionRan);
         _ = CriticalSection.TryUpdateWhenAsync(() => true, () => _actionRan = true);
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_other_wait_completes_returning_true() => (await _otherWait.WaitAsync(Patience)).Must().BeTrue();
   }

   public class when_a_synchronous_wait_waits_for_what_its_action_does : TryUpdateWhenAsync
   {
      bool _actionRan;
      readonly ManualResetEventSlim _theSynchronousWaitHasEvaluatedItsCondition = new();
      readonly Task<bool> _synchronousWait;

      public when_a_synchronous_wait_waits_for_what_its_action_does()
      {
         _synchronousWait = TaskCE.Run(() => CriticalSection.TryAwait(() =>
                                                                      {
                                                                         _theSynchronousWaitHasEvaluatedItsCondition.Set();
                                                                         return _actionRan;
                                                                      },
                                                                      waitTimeout: WaitTimeout.Seconds(30)));
         _theSynchronousWaitHasEvaluatedItsCondition.Wait();
         _ = CriticalSection.TryUpdateWhenAsync(() => true, () => _actionRan = true);
      }

      protected override async Task DisposeAsyncInternal()
      {
         await _synchronousWait.AwaitCompletionWithinAsync(Patience);
         _theSynchronousWaitHasEvaluatedItsCondition.Dispose();
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_synchronous_wait_returns_true() => (await _synchronousWait.WaitAsync(Patience)).Must().BeTrue();
   }

   ///<summary>An update wakes every wait, so a wait whose condition is false must not release the lock as an update: two such waits would wake each other forever.</summary>
   public class when_two_waits_wait_for_conditions_that_do_not_hold : TryUpdateWhenAsync
   {
      bool _released;
      int _firstConditionEvaluations;
      int _secondConditionEvaluations;
      readonly Task<bool> _firstWait;
      readonly Task<bool> _secondWait;

      public when_two_waits_wait_for_conditions_that_do_not_hold()
      {
         _firstWait = CriticalSection.TryUpdateWhenAsync(TheFirstCondition, () => {});
         _secondWait = CriticalSection.TryUpdateWhenAsync(TheSecondCondition, () => {});
         return;

         bool TheFirstCondition()
         {
            _firstConditionEvaluations++;
            return _released;
         }

         bool TheSecondCondition()
         {
            _secondConditionEvaluations++;
            return _released;
         }
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await Task.Delay(TimeAWokenWaitWouldHaveCompletedIn);
      }

      protected override async Task DisposeAsyncInternal()
      {
         CriticalSection.Update(() => _released = true);
         await Task.WhenAll(_firstWait, _secondWait).AwaitCompletionWithinAsync(Patience);
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public void neither_condition_is_evaluated_again() =>
         CriticalSection.Read(() => (_firstConditionEvaluations, _secondConditionEvaluations)).Must().Be((1, 1));
   }

   public class when_the_action_throws : TryUpdateWhenAsync
   {
      readonly Task<bool> _wait;
      public when_the_action_throws() => _wait = CriticalSection.TryUpdateWhenAsync(() => true, () => throw new IntentionalSpecificationException());

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_with_the_exception_the_action_threw() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<IntentionalSpecificationException>();

      [IAwaitableCriticalSectionMatrix] public void another_thread_can_take_the_lock_afterwards() => AnotherThreadCanTakeTheLock().Must().BeTrue();
   }

   public class when_cancelled_while_waiting : TryUpdateWhenAsync
   {
      readonly CancellationTokenSource _cancellation = new();
      readonly Task<bool> _wait;

      public when_cancelled_while_waiting()
      {
         _wait = CriticalSection.TryUpdateWhenAsync(() => false, () => {}, _cancellation.Token);
         _cancellation.Cancel();
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _wait.AwaitCompletionWithinAsync(Patience);
      }

      protected override async Task DisposeAsyncInternal()
      {
         _cancellation.Dispose();
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_with_an_OperationCanceledException() =>
         (await InvokingAsync(() => _wait).Must().ThrowAsync<OperationCanceledException>()).Which.GetType().Must().Be(typeof(OperationCanceledException));

      [IAwaitableCriticalSectionMatrix] public void another_thread_can_take_the_lock_afterwards() => AnotherThreadCanTakeTheLock().Must().BeTrue();
   }

   public class when_the_thread_starting_it_holds_the_critical_section : TryUpdateWhenAsync
   {
      readonly Task<bool> _wait;

      public when_the_thread_starting_it_holds_the_critical_section()
      {
         using(CriticalSection.TakeUpdateLock()) _wait = CriticalSection.TryUpdateWhenAsync(() => true, () => {});
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_at_once_with_a_StateAssertionFailedException() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<StateAssertionFailedException>();
   }
}
