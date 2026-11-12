using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class ReadWhenAsync : AsyncSharedWaitSpecification
{
   public class when_an_update_makes_the_condition_true : ReadWhenAsync
   {
      readonly Task<int> _read;

      public when_an_update_makes_the_condition_true()
      {
         _read = Shared.ReadWhenAsync(it => it.Items.Count > 0, it => it.Items[0]);
         Shared.Update(it => it.Items.Add(99));
      }

      [IAwaitableSharedMatrix] public async Task it_returns_what_the_read_read_from_the_shared_object() => (await _read.WaitAsync(Patience)).Must().Be(99);
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : ReadWhenAsync
   {
      readonly Task<int> _read;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _read = Shared.ReadWhenAsync(_ => false, it => it.Value, timeout: AShortWaitTimeout);

      [IAwaitableSharedMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _read).Must().ThrowAsync<AwaitingConditionTimeoutException>();
   }
}
