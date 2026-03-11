using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Threading.Exceptions;

namespace Compze.Threading.Testing;

public partial interface IThreadGate
{
   ///<summary>Blocks until <see cref="condition"/> returns true or <paramref name="timeout"/> expires. Throws <exception cref="AwaitingConditionTimeoutException" /> if <paramref name="timeout"/> expires. Uses <see cref="WaitTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IThreadGate Await(Func<IThreadGate, bool> condition, WaitTimeout? timeout = null) => ExecuteWithExclusiveLockWhen(condition, () => {}, timeout);

   ///<summary>Blocks until <see cref="IsOpen"/> becomes false or <paramref name="timeout"/> expires. Throws <exception cref="AwaitingConditionTimeoutException" /> if <paramref name="timeout"/> expires. Uses <see cref="WaitTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IThreadGate AwaitClosed(WaitTimeout? timeout = null) => Await(@this => !@this.IsOpen, timeout);

   ///<summary>Blocks until <see cref="Queued"/> equal <paramref name="count"/> or <paramref name="timeout"/> expires. Throws <exception cref="AwaitingConditionTimeoutException" /> if <paramref name="timeout"/> expires first. Uses <see cref="WaitTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IThreadGate AwaitQueueLengthEqualTo(int count, WaitTimeout? timeout = null) => Await(@this => @this.Queued == count, timeout);
   ///<summary>Blocks until <see cref="Queued"/> equal <paramref name="count"/> or <paramref name="timeout"/> expires. Returns false if <paramref name="timeout"/> expires, else true.</summary>
   bool TryAwaitQueueLengthEqualTo(int count, WaitTimeout? timeout = null) => TryAwait(@this => @this.Queued == count, timeout);

   ///<summary>Blocks until <see cref="Passed"/> equal <paramref name="count"/> or <paramref name="timeout"/> expires. Throws <exception cref="AwaitingConditionTimeoutException" /> if <paramref name="timeout"/> expires first. Uses <see cref="WaitTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IThreadGate AwaitPassedThroughCountEqualTo(int count, WaitTimeout? timeout = null) => Await(@this => @this.Passed == count, timeout);
   ///<summary>Blocks until <see cref="Passed"/> equal <paramref name="count"/> or <paramref name="timeout"/> expires. Returns false if <paramref name="timeout"/> expires, else true.</summary>
   bool TryAwaitPassedThroughCountEqualTo(int count, WaitTimeout? timeout = null) => TryAwait(@this => @this.Passed == count, timeout);

   ///<summary>Injects an action that throws <paramref name="exception"/> when the next thread exits <see cref="IThreadGateVisitor.AwaitPassThrough"/></summary>
   IThreadGate ThrowPostPassThrough(Exception exception) => SetPostPassThroughAction(_ => throw exception);

   ///<summary>Injects an action that throws <paramref name="exception"/> curren transaction of the next thread to call <see cref="IThreadGateVisitor.AwaitPassThrough"/> completes.</summary>
   IThreadGate FailTransactionOnPreparePostPassThrough(Exception exception) => SetPostPassThroughAction(_ =>
   {
      State.NotNull(Transaction.Current);
      Transaction.Current.FailOnPrepare(exception);
   });
}
