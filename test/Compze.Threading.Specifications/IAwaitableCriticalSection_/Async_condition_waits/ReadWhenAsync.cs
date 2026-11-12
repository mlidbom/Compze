using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class ReadWhenAsync : AsyncConditionWaitSpecification
{
   public class when_an_update_makes_the_condition_true : ReadWhenAsync
   {
      int _value;
      bool _anotherThreadCouldTakeTheLockWhileTheFuncRan;
      readonly Task<int> _read;

      public when_an_update_makes_the_condition_true()
      {
         _read = CriticalSection.ReadWhenAsync(() => _value > 0, () =>
         {
            _anotherThreadCouldTakeTheLockWhileTheFuncRan = AnotherThreadCanTakeTheLock();
            return _value;
         });
         CriticalSection.Update(() => _value = 7);
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _read.AwaitCompletionWithinAsync(Patience);
      }

      [IAwaitableCriticalSectionMatrix] public async Task it_returns_what_the_func_read() => (await _read).Must().Be(7);
      [IAwaitableCriticalSectionMatrix] public void no_other_thread_could_take_the_lock_while_the_func_ran() => _anotherThreadCouldTakeTheLockWhileTheFuncRan.Must().BeFalse();
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : ReadWhenAsync
   {
      readonly Task<int> _read;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _read = CriticalSection.ReadWhenAsync(() => false, () => 1, waitTimeout: WaitTimeout.Milliseconds(100));

      [IAwaitableCriticalSectionMatrix] public async Task it_fails_with_an_AwaitingConditionTimeoutException() =>
         await InvokingAsync(() => _read).Must().ThrowAsync<AwaitingConditionTimeoutException>();
   }
}
