using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits.UpdateWhenAsync;

[Collection(nameof(NonParallelCollection))]
public class with_a_func : AsyncConditionWaitSpecification
{
   public class when_an_update_makes_the_condition_true : with_a_func
   {
      bool _conditionHolds;
      readonly Task<int> _update;

      public when_an_update_makes_the_condition_true()
      {
         _update = CriticalSection.UpdateWhenAsync(() => _conditionHolds, () => 42);
         CriticalSection.Update(() => _conditionHolds = true);
      }

      [IAwaitableCriticalSectionMatrix] public async Task it_returns_what_the_func_returned() => (await _update.WaitAsync(Patience)).Must().Be(42);
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : with_a_func
   {
      readonly Task<int> _update;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _update = CriticalSection.UpdateWhenAsync(() => false, () => 42, waitTimeout: WaitTimeout.Milliseconds(100));

      [IAwaitableCriticalSectionMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _update).Must().ThrowAsync<AwaitingConditionTimeoutException>();
   }
}
