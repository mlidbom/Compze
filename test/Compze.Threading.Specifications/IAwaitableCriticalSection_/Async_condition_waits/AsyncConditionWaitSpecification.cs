using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_.Async_condition_waits;

///<summary>What the specifications of <see cref="IAwaitableCriticalSection"/>'s async condition waits start from: a critical section of the matrix's<br/>
/// current implementation, whose timeouts only a broken implementation reaches.</summary>
///<remarks>A specification that leaves a wait pending ends it before calling <see cref="DisposeAsyncInternal"/>, so no wait outlives the critical section it waits on.</remarks>
[Collection(nameof(NonParallelCollection))]
public abstract class AsyncConditionWaitSpecification : UniversalTestBase
{
   ///<summary>How long a specification waits for something that should happen: only a broken implementation takes this long.</summary>
   protected static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

   ///<summary>How long a specification gives a wait to complete before concluding that nothing woke it: a woken wait completes in far less.</summary>
   protected static readonly TimeSpan TimeAWokenWaitWouldHaveCompletedIn = TimeSpan.FromMilliseconds(100);

   private protected IAwaitableCriticalSectionMatrixAttribute.Factory<AsyncConditionWaitSpecification> Factory { get; } = new();

   protected IAwaitableCriticalSection CriticalSection { get; }

   protected AsyncConditionWaitSpecification() => CriticalSection = Factory.Create(LockTimeout.Seconds(30), WaitTimeout.Seconds(30));

   ///<summary>True if a thread other than the calling one can take an update lock on <see cref="CriticalSection"/> right now, waiting at most 50 milliseconds.<br/>
   /// Asked from another thread because the calling thread could take a lock it already holds.</summary>
   protected bool AnotherThreadCanTakeTheLock()
   {
      var lockTaken = false;
      var otherThread = new Thread(TryTakeTheLock) { IsBackground = true, Name = nameof(AnotherThreadCanTakeTheLock) };
      otherThread.Start();
      otherThread.Join();
      return lockTaken;

      void TryTakeTheLock()
      {
         try
         {
            CriticalSection.TakeUpdateLock(timeout: LockTimeout.Milliseconds(50)).Dispose();
            lockTaken = true;
         }
         catch(TakeLockTimeoutException) {}
      }
   }

   protected override async Task DisposeAsyncInternal()
   {
      await base.DisposeAsyncInternal();
      Factory.Dispose();
   }
}
