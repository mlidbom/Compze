using Compze.Must;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class TryUpdateWhenAsync : AsyncSharedWaitSpecification
{
   public class when_an_update_makes_the_condition_true : TryUpdateWhenAsync
   {
      readonly Task<bool> _update;

      public when_an_update_makes_the_condition_true()
      {
         _update = Shared.TryUpdateWhenAsync(it => it.Items.Count > 0, it => it.Value = 7);
         Shared.Update(it => it.Items.Add(1));
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _update.WaitAsync(Patience);
      }

      [IAwaitableSharedMatrix] public async Task it_returns_true() => (await _update).Must().BeTrue();
      [IAwaitableSharedMatrix] public void the_shared_object_holds_the_update() => Shared.Read(it => it.Value).Must().Be(7);
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : TryUpdateWhenAsync
   {
      readonly Task<bool> _update;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _update = Shared.TryUpdateWhenAsync(_ => false, it => it.Value = 7, timeout: AShortWaitTimeout);

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _update.WaitAsync(Patience);
      }

      [IAwaitableSharedMatrix] public async Task it_returns_false() => (await _update).Must().BeFalse();
      [IAwaitableSharedMatrix] public void the_shared_object_is_unchanged() => Shared.Read(it => it.Value).Must().Be(0);
   }
}
