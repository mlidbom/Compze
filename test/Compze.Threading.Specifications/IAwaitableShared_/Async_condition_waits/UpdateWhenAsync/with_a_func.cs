using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits.UpdateWhenAsync;

[Collection(nameof(NonParallelCollection))]
public class with_a_func : AsyncSharedWaitSpecification
{
   public class when_an_update_makes_the_condition_true : with_a_func
   {
      readonly Task<int> _update;

      public when_an_update_makes_the_condition_true()
      {
         _update = Shared.UpdateWhenAsync(it => it.Items.Count > 0, it => it.Value = it.Items[0] + 1);
         Shared.Update(it => it.Items.Add(41));
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _update.WaitAsync(Patience);
      }

      [IAwaitableSharedMatrix] public async Task it_returns_what_the_update_returned() => (await _update).Must().Be(42);
      [IAwaitableSharedMatrix] public void the_shared_object_holds_the_update() => Shared.Read(it => it.Value).Must().Be(42);
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : with_a_func
   {
      readonly Task<int> _update;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _update = Shared.UpdateWhenAsync(_ => false, it => it.Value = 1, timeout: AShortWaitTimeout);

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _update.AwaitCompletionWithinAsync(Patience);
      }

      [IAwaitableSharedMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _update).Must().ThrowAsync<AwaitingConditionTimeoutException>();

      [IAwaitableSharedMatrix] public void the_shared_object_is_unchanged() => Shared.Read(it => it.Value).Must().Be(0);
   }
}
