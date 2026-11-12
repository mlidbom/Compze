using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_;

///<summary>A condition can become true without an update to wake the waits on it, for instance when the state it reads changes outside the critical section's<br/>
/// update lock. A wait that times out must still have seen its condition false, so it evaluates the condition once more when its wait timeout expires.</summary>
[Collection(nameof(NonParallelCollection))]
public class A_condition_that_becomes_true_without_an_update_before_the_wait_timeout_expires : UniversalTestBase
{
   static readonly WaitTimeout TheWaitTimeout = WaitTimeout.Milliseconds(300);
   static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

   readonly IAwaitableCriticalSectionMatrixAttribute.Factory<A_condition_that_becomes_true_without_an_update_before_the_wait_timeout_expires> _factory = new();
   readonly ManualResetEventSlim _theConditionHasBeenEvaluated = new();
   readonly IAwaitableCriticalSection _criticalSection;
   bool _conditionHolds;

   protected A_condition_that_becomes_true_without_an_update_before_the_wait_timeout_expires() => _criticalSection = _factory.Create(LockTimeout.Seconds(30));

   protected override void DisposeInternal()
   {
      _theConditionHasBeenEvaluated.Dispose();
      _factory.Dispose();
   }

   bool TheCondition()
   {
      _theConditionHasBeenEvaluated.Set();
      return Volatile.Read(ref _conditionHolds);
   }

   ///<summary>Makes the condition true without taking the lock, once the wait has evaluated it, so nothing wakes the wait.</summary>
   void MakeTheConditionTrueWithoutAnUpdate()
   {
      _theConditionHasBeenEvaluated.Wait();
      Volatile.Write(ref _conditionHolds, true);
   }

   public class awaited_synchronously : A_condition_that_becomes_true_without_an_update_before_the_wait_timeout_expires
   {
      readonly Task<bool> _wait;

      public awaited_synchronously()
      {
         _wait = TaskCE.RunOnDedicatedThread(() => _criticalSection.TryAwait(TheCondition, waitTimeout: TheWaitTimeout));
         MakeTheConditionTrueWithoutAnUpdate();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_returns_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
   }

   public class awaited_asynchronously : A_condition_that_becomes_true_without_an_update_before_the_wait_timeout_expires
   {
      readonly Task<bool> _wait;

      public awaited_asynchronously()
      {
         _wait = _criticalSection.TryAwaitAsync(TheCondition, waitTimeout: TheWaitTimeout);
         MakeTheConditionTrueWithoutAnUpdate();
      }

      [IAwaitableCriticalSectionMatrix] public async Task the_wait_returns_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
   }
}
