using Compze.Tests.Infrastructure;
using System.Diagnostics;
using Compze.Must;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class TryAwaitAsync : AsyncSharedWaitSpecification
{
   public class when_the_condition_already_holds : TryAwaitAsync
   {
      readonly Task<bool> _wait;
      public when_the_condition_already_holds() => _wait = Shared.TryAwaitAsync(_ => true);

      [IAwaitableSharedMatrix] public void the_wait_has_completed_by_the_time_the_call_returns() => _wait.IsCompletedSuccessfully.Must().BeTrue();
   }

   public class when_an_update_makes_the_condition_true : TryAwaitAsync
   {
      readonly Task<bool> _wait;

      public when_an_update_makes_the_condition_true()
      {
         _wait = Shared.TryAwaitAsync(it => it.Items.Count > 0);
         Shared.Update(it => it.Items.Add(1));
      }

      [IAwaitableSharedMatrix] public async Task the_wait_completes_returning_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : TryAwaitAsync
   {
      readonly Task<bool> _wait;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _wait = Shared.TryAwaitAsync(_ => false, timeout: AShortWaitTimeout);

      [IAwaitableSharedMatrix] public async Task the_wait_returns_false() => (await _wait.WaitAsync(Patience)).Must().BeFalse();
   }

   public class when_far_more_waits_wait_than_the_thread_pool_has_threads : TryAwaitAsync
   {
      const int NumberOfWaits = 1000;
      readonly int _processThreadsBeforeTheWaits;
      readonly int _processThreadsWhileTheyWait;
      readonly IReadOnlyList<Task<bool>> _waits;

      public when_far_more_waits_wait_than_the_thread_pool_has_threads()
      {
         _processThreadsBeforeTheWaits = ProcessThreadCount();
         _waits = [..Enumerable.Range(0, NumberOfWaits).Select(_ => Shared.TryAwaitAsync(it => it.Items.Count > 0))];
         _processThreadsWhileTheyWait = ProcessThreadCount();
      }

      protected override async Task DisposeAsyncInternal()
      {
         Shared.Update(it => it.Items.Add(1));
         await Task.WhenAll(_waits).AwaitCompletionWithinAsync(Patience);
         await base.DisposeAsyncInternal();
      }

      [IAwaitableSharedMatrix] public void the_waits_do_not_hold_a_thread_each() =>
         (_processThreadsWhileTheyWait - _processThreadsBeforeTheWaits).Must().BeLessThan(NumberOfWaits / 10);

      static int ProcessThreadCount()
      {
         using var process = Process.GetCurrentProcess();
         return process.Threads.Count;
      }
   }
}
