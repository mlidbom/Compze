using Compze.Tests.Infrastructure;
using System.Diagnostics;
using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits;

[Collection(nameof(NonParallelCollection))]
public class TryAwaitAsync : AsyncConditionWaitSpecification
{
   public class when_the_condition_already_holds : TryAwaitAsync
   {
      readonly Task<bool> _wait;
      public when_the_condition_already_holds() => _wait = CriticalSection.TryAwaitAsync(() => true);

      [IAwaitableCriticalSectionMatrix] public void the_wait_has_completed_by_the_time_the_call_returns() => _wait.IsCompletedSuccessfully.Must().BeTrue();
      [IAwaitableCriticalSectionMatrix] public async Task the_wait_returns_true() => (await _wait).Must().BeTrue();
   }

   public class when_the_condition_does_not_hold : TryAwaitAsync
   {
      bool _conditionHolds;
      int _conditionEvaluations;
      readonly Task<bool> _wait;

      public when_the_condition_does_not_hold() => _wait = CriticalSection.TryAwaitAsync(() =>
      {
         _conditionEvaluations++;
         return _conditionHolds;
      });

      protected override async Task DisposeAsyncInternal()
      {
         CriticalSection.Update(() => _conditionHolds = true);
         await _wait.AwaitCompletionWithinAsync(Patience);
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_does_not_complete() => (await _wait.CompletesWithinAsync(TimeAWokenWaitWouldHaveCompletedIn)).Must().BeFalse();

      public class and_an_update_makes_it_true : when_the_condition_does_not_hold
      {
         public and_an_update_makes_it_true() => CriticalSection.Update(() => _conditionHolds = true);

         [IAwaitableCriticalSectionMatrix] public async Task the_wait_completes_returning_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
      }

      public class and_an_update_leaves_it_false : when_the_condition_does_not_hold
      {
         public and_an_update_leaves_it_false() => CriticalSection.Update(() => {});

         [IAwaitableCriticalSectionMatrix] public async Task the_wait_still_does_not_complete() => (await _wait.CompletesWithinAsync(TimeAWokenWaitWouldHaveCompletedIn)).Must().BeFalse();
      }

      public class and_a_read_lock_is_released : when_the_condition_does_not_hold
      {
         public and_a_read_lock_is_released() => CriticalSection.TakeReadLock().Dispose();

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await Task.Delay(TimeAWokenWaitWouldHaveCompletedIn);
         }

         [IAwaitableCriticalSectionMatrix] public void the_condition_is_not_evaluated_again() => CriticalSection.Read(() => _conditionEvaluations).Must().Be(1);
      }
   }

   public class when_the_condition_does_not_hold_before_the_wait_timeout_expires : TryAwaitAsync
   {
      readonly Task<bool> _wait;
      public when_the_condition_does_not_hold_before_the_wait_timeout_expires() => _wait = CriticalSection.TryAwaitAsync(() => false, waitTimeout: WaitTimeout.Milliseconds(100));

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_returns_false() => (await _wait.WaitAsync(Patience)).Must().BeFalse();
   }

   public class when_far_more_waits_wait_than_the_thread_pool_has_threads : TryAwaitAsync
   {
      const int NumberOfWaits = 1000;
      bool _released;
      readonly int _processThreadsBeforeTheWaits;
      readonly int _processThreadsWhileTheyWait;
      readonly IReadOnlyList<Task<bool>> _waits;

      public when_far_more_waits_wait_than_the_thread_pool_has_threads()
      {
         _processThreadsBeforeTheWaits = ProcessThreadCount();
         _waits = OnANewThread(() => Enumerable.Range(0, NumberOfWaits).Select(_ => CriticalSection.TryAwaitAsync(() => _released)).ToList());
         _processThreadsWhileTheyWait = ProcessThreadCount();
      }

      protected override async Task DisposeAsyncInternal()
      {
         CriticalSection.Update(() => _released = true);
         await Task.WhenAll(_waits).AwaitCompletionWithinAsync(Patience);
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public void the_waits_do_not_hold_a_thread_each() =>
         (_processThreadsWhileTheyWait - _processThreadsBeforeTheWaits).Must().BeLessThan(NumberOfWaits / 10);

      ///<summary>What starves the thread pool when waits park its threads: the update the waits wait for needs a pool thread to run on, and every one is parked.</summary>
      public class and_the_update_they_wait_for_is_queued_on_the_thread_pool_behind_them : when_far_more_waits_wait_than_the_thread_pool_has_threads
      {
         readonly Task _update;
         public and_the_update_they_wait_for_is_queued_on_the_thread_pool_behind_them() => _update = OnANewThread(() => Task.Run(() => CriticalSection.Update(() => _released = true)));

         [IAwaitableCriticalSectionMatrix] public async Task the_update_and_every_wait_complete() =>
            (await Task.WhenAll([.._waits, _update]).CompletesWithinAsync(Patience)).Must().BeTrue();
      }

      static int ProcessThreadCount()
      {
         using var process = Process.GetCurrentProcess();
         return process.Threads.Count;
      }

      ///<summary>Runs <paramref name="func"/> on a new thread outside the thread pool, so whatever it queues on the pool goes to the pool's one first-in-first-out<br/>
      /// queue: a pool thread puts what it queues on its own queue instead, and takes it back out last-in-first-out, ahead of what it queued earlier.</summary>
      static TResult OnANewThread<TResult>(Func<TResult> func)
      {
         var result = default(TResult)!;
         var thread = new Thread(() => result = func()) { IsBackground = true, Name = nameof(OnANewThread) };
         thread.Start();
         thread.Join();
         return result;
      }
   }

   public class when_cancelled_while_waiting : TryAwaitAsync
   {
      readonly CancellationTokenSource _cancellation = new();
      readonly Task<bool> _wait;

      public when_cancelled_while_waiting()
      {
         _wait = CriticalSection.TryAwaitAsync(() => false, _cancellation.Token);
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

   public class when_the_condition_throws : TryAwaitAsync
   {
      readonly Task<bool> _wait;
      public when_the_condition_throws() => _wait = CriticalSection.TryAwaitAsync(() => throw new IntentionalSpecificationException());

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_with_the_exception_the_condition_threw() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<IntentionalSpecificationException>();

      [IAwaitableCriticalSectionMatrix] public void another_thread_can_take_the_lock_afterwards() => AnotherThreadCanTakeTheLock().Must().BeTrue();
   }

   public class when_the_condition_throws_once_an_update_has_it_evaluated_again : TryAwaitAsync
   {
      bool _throwWhenEvaluated;
      readonly Task<bool> _wait;

      public when_the_condition_throws_once_an_update_has_it_evaluated_again()
      {
         _wait = CriticalSection.TryAwaitAsync(() => _throwWhenEvaluated ? throw new IntentionalSpecificationException() : false);
         CriticalSection.Update(() => _throwWhenEvaluated = true);
      }

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _wait.AwaitCompletionWithinAsync(Patience);
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_with_the_exception_the_condition_threw() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<IntentionalSpecificationException>();

      [IAwaitableCriticalSectionMatrix] public void another_thread_can_take_the_lock_afterwards() => AnotherThreadCanTakeTheLock().Must().BeTrue();
   }

   public class when_the_thread_starting_it_holds_the_critical_section : TryAwaitAsync
   {
      readonly Task<bool> _wait;

      public when_the_thread_starting_it_holds_the_critical_section()
      {
         using(CriticalSection.TakeUpdateLock()) _wait = CriticalSection.TryAwaitAsync(() => true);
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_at_once_with_a_StateAssertionFailedException() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<StateAssertionFailedException>();
   }

   public class when_another_thread_holds_the_lock_longer_than_the_lock_timeout : TryAwaitAsync
   {
      readonly ManualResetEventSlim _theLockIsHeld = new();
      readonly ManualResetEventSlim _releaseTheLock = new();
      readonly Thread _lockHolder;
      readonly Task<bool> _wait;

      public when_another_thread_holds_the_lock_longer_than_the_lock_timeout()
      {
         _lockHolder = new Thread(HoldTheLockUntilReleased) { IsBackground = true, Name = nameof(HoldTheLockUntilReleased) };
         _lockHolder.Start();
         _theLockIsHeld.Wait();

         _wait = CriticalSection.TryAwaitAsync(() => true, lockTimeout: LockTimeout.Milliseconds(50));
      }

      void HoldTheLockUntilReleased()
      {
         using(CriticalSection.TakeUpdateLock())
         {
            _theLockIsHeld.Set();
            _releaseTheLock.Wait();
         }
      }

      protected override async Task DisposeAsyncInternal()
      {
         _releaseTheLock.Set();
         _lockHolder.Join();
         _releaseTheLock.Dispose();
         _theLockIsHeld.Dispose();
         await base.DisposeAsyncInternal();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_fails_with_a_TakeLockTimeoutException() =>
         await InvokingAsync(() => _wait).Must().ThrowAsync<TakeLockTimeoutException>();
   }
}
