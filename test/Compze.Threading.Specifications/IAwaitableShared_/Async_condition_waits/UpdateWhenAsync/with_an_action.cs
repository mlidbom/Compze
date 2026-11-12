using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits.UpdateWhenAsync;

[Collection(nameof(NonParallelCollection))]
public class with_an_action : AsyncSharedWaitSpecification
{
   public class when_an_update_makes_the_condition_true : with_an_action
   {
      readonly Task _update;

      public when_an_update_makes_the_condition_true()
      {
         //A statement-block lambda, so overload resolution picks the Action-taking UpdateWhenAsync under specification: the expression form would bind to the Func overload.
         _update = Shared.UpdateWhenAsync(it => it.Items.Count > 0, it => { it.Items.Add(2); });
         Shared.Update(it => it.Items.Add(1));
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _update.WaitAsync(Patience);
      }

      [IAwaitableSharedMatrix] public void the_shared_object_holds_the_update() => Shared.Read(it => it.Items.ToList()).Must().SequenceEqual([1, 2]);
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : with_an_action
   {
      readonly Task _update;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _update = Shared.UpdateWhenAsync(_ => false, it => { it.Items.Add(1); }, timeout: AShortWaitTimeout);

      [IAwaitableSharedMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _update).Must().ThrowAsync<AwaitingConditionTimeoutException>();
   }
}
