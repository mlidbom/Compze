using Compze.Tests.Infrastructure;
using Compze.Threading.ResourceAccess;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;

namespace Compze.Threading.Specifications.IAwaitableShared_.Async_condition_waits;

///<summary>What the specifications of <see cref="IAwaitableShared{TShared}"/>'s async condition waits start from: a shared <see cref="SharedTestValue"/><br/>
/// in the matrix's current implementation, with no items and a value of 0, whose timeouts only a broken implementation reaches.</summary>
[Collection(nameof(NonParallelCollection))]
public abstract class AsyncSharedWaitSpecification : UniversalTestBase
{
   ///<summary>How long a specification waits for something that should happen: only a broken implementation takes this long.</summary>
   protected static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

   ///<summary>A wait timeout short enough that a specification of what happens when it expires doesn't wait long.</summary>
   protected static readonly WaitTimeout AShortWaitTimeout = WaitTimeout.Milliseconds(100);

   private protected IAwaitableSharedMatrixAttribute.Factory<AsyncSharedWaitSpecification> Factory { get; } = new();

   private protected IAwaitableShared<SharedTestValue> Shared { get; }

   protected AsyncSharedWaitSpecification() => Shared = Factory.Create(new SharedTestValue(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));

   protected override async Task DisposeAsyncInternal()
   {
      await base.DisposeAsyncInternal();
      Factory.Dispose();
   }
}
