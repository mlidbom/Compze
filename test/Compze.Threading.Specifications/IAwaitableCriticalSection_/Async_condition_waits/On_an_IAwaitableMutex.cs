using Compze.Must;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitMatrix;
using Xunit;
using static Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure.IAwaitableCriticalSectionMatrixAttribute;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits;

///<summary>What only a mutex has: other instances of the same system mutex, as other processes open it, and holders that can die holding it.</summary>
[Collection(nameof(NonParallelCollection))]
public class On_an_IAwaitableMutex : AsyncConditionWaitSpecification
{
   const string AMonitorHasNeither = "An in-process monitor has neither other instances nor holders that can die holding it.";

   IAwaitableMutex MutexUnderSpecification => (IAwaitableMutex)CriticalSection;

   public class an_async_wait_whose_condition_another_instance_with_the_same_name_makes_true : On_an_IAwaitableMutex
   {
      bool _conditionHolds;
      readonly Task<bool> _wait;

      public an_async_wait_whose_condition_another_instance_with_the_same_name_makes_true()
      {
         _wait = MutexUnderSpecification.TryAwaitAsync(() => _conditionHolds);
         Factory.CreateAnotherInstanceOf(MutexUnderSpecification).Update(() => _conditionHolds = true);
      }

      [IAwaitableCriticalSectionMatrix, Skip<Implementation>(Implementation.Monitor, AMonitorHasNeither)]
      public async Task completes_returning_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
   }

   ///<summary>A holder that dies holding the mutex never releases it, so never raises the signal its waits wait for: they find out by probing the mutex.</summary>
   public class an_async_wait_whose_condition_a_holder_that_dies_holding_the_mutex_makes_true : On_an_IAwaitableMutex
   {
      bool _conditionHolds;
      readonly Task<bool> _wait;

      public an_async_wait_whose_condition_a_holder_that_dies_holding_the_mutex_makes_true()
      {
         _wait = MutexUnderSpecification.TryAwaitAsync(() => _conditionHolds);
         var holder = new Thread(MakeTheConditionTrueAndDieHoldingTheMutex) { IsBackground = true, Name = nameof(MakeTheConditionTrueAndDieHoldingTheMutex) };
         holder.Start();
         holder.Join();
      }

      void MakeTheConditionTrueAndDieHoldingTheMutex()
      {
#pragma warning disable CA2000 // The holder dies holding the lock on purpose: abandoning the mutex is what this specifies.
         MutexUnderSpecification.TakeUpdateLock();
#pragma warning restore CA2000
         _conditionHolds = true;
      }

      [IAwaitableCriticalSectionMatrix, Skip<Implementation>(Implementation.Monitor, AMonitorHasNeither)]
      public async Task completes_returning_true() => (await _wait.WaitAsync(Patience)).Must().BeTrue();
   }
}
