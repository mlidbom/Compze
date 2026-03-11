using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE.Testing;

namespace Compze.Threading.Testing;

public partial interface IThreadGate
{
   IThreadGate Await(Func<bool> condition) => Await(WaitTimeout, condition);
   IThreadGate Await(WaitTimeout? timeout, Func<bool> condition) => ExecuteWithExclusiveLockWhen(condition, () => {}, timeout);

   IThreadGate AwaitClosed() => Await(() => !IsOpen);
   IThreadGate AwaitQueueLengthEqualTo(int length) => Await(() => Queued == length);
   IThreadGate AwaitQueueLengthEqualTo(int length, WaitTimeout timeout) => Await(timeout, () => Queued == length);
   bool TryAwaitQueueLengthEqualTo(int length, WaitTimeout timeout) => TryAwait(() => Queued == length, timeout);
   IThreadGate AwaitPassedThroughCountEqualTo(int length) => Await(() => Passed == length);
   IThreadGate AwaitPassedThroughCountEqualTo(int length, WaitTimeout? timeout) => Await(timeout, () => Passed == length);
   bool TryAwaitPassedThroughCountEqualTo(int count, WaitTimeout? timeout = null) => TryAwait(() => Passed == count, timeout);
   IThreadGate ThrowPostPassThrough(Exception exception) => SetPostPassThroughAction(_ => throw exception);

   IThreadGate FailTransactionOnPreparePostPassThrough(Exception exception) => SetPostPassThroughAction(_ =>
   {
      State.NotNull(Transaction.Current);
      Transaction.Current.FailOnPrepare(exception);
   });
}
