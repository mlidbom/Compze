using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE.Testing;

namespace Compze.Threading.Testing;

public partial interface IThreadGate
{
   IThreadGate Await(Func<IThreadGate, bool> condition, WaitTimeout? timeout = null) => ExecuteWithExclusiveLockWhen(condition, () => {}, timeout);

   IThreadGate AwaitClosed(WaitTimeout? timeout = null) => Await(@this => !@this.IsOpen, timeout);

   IThreadGate AwaitQueueLengthEqualTo(int length, WaitTimeout? timeout = null) => Await(@this => @this.Queued == length, timeout);
   bool TryAwaitQueueLengthEqualTo(int length, WaitTimeout? timeout = null) => TryAwait(@this => @this.Queued == length, timeout);

   IThreadGate AwaitPassedThroughCountEqualTo(int length, WaitTimeout? timeout = null) => Await(@this => @this.Passed == length, timeout);
   bool TryAwaitPassedThroughCountEqualTo(int count, WaitTimeout? timeout = null) => TryAwait(@this => @this.Passed == count, timeout);

   IThreadGate ThrowPostPassThrough(Exception exception) => SetPostPassThroughAction(_ => throw exception);

   IThreadGate FailTransactionOnPreparePostPassThrough(Exception exception) => SetPostPassThroughAction(_ =>
   {
      State.NotNull(Transaction.Current);
      Transaction.Current.FailOnPrepare(exception);
   });
}
