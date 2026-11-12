using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class AwaitAsync : AsyncSharedWaitSpecification
{
   public class when_an_update_makes_the_condition_true : AwaitAsync
   {
      readonly Task _wait;

      public when_an_update_makes_the_condition_true()
      {
         _wait = Shared.AwaitAsync(it => it.Items.Count > 0);
         Shared.Update(it => it.Items.Add(1));
      }

      [IAwaitableSharedMatrix] public async Task the_wait_completes() => (await _wait.CompletesWithinAsync(Patience)).Must().BeTrue();
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : AwaitAsync
   {
      readonly Task _wait;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _wait = Shared.AwaitAsync(_ => false, timeout: AShortWaitTimeout);

      [IAwaitableSharedMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<AwaitingConditionTimeoutException>();
   }
}
