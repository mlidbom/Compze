using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits.UpdateWhenAsync;

[Collection(nameof(NonParallelCollection))]
public class with_an_action : AsyncConditionWaitSpecification
{
   public class when_an_update_makes_the_condition_true : with_an_action
   {
      bool _conditionHolds;
      bool _actionRan;
      readonly Task _update;

      public when_an_update_makes_the_condition_true()
      {
         //A statement-block lambda, so overload resolution picks the Action-taking UpdateWhenAsync under specification: the expression form would bind to the Func overload.
         _update = CriticalSection.UpdateWhenAsync(() => _conditionHolds, () => { _actionRan = true; });
         CriticalSection.Update(() => _conditionHolds = true);
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _update.WaitAsync(Patience);
      }

      [IAwaitableCriticalSectionMatrix] public void the_action_has_run() => _actionRan.Must().BeTrue();
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : with_an_action
   {
      readonly Task _update;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _update = CriticalSection.UpdateWhenAsync(() => false, () => {}, waitTimeout: WaitTimeout.Milliseconds(100));

      [IAwaitableCriticalSectionMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _update).Must().ThrowAsync<AwaitingConditionTimeoutException>();
   }
}
