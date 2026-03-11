using System.Transactions;

namespace Compze.Threading.Testing;

///<summary>A snapshot of a <see cref="Transaction"/>'s state at a point in time.</summary>
public class TransactionSnapshot(Transaction transaction)
{
   ///<summary>The <see cref="System.Transactions.IsolationLevel"/> of the transaction at the time the snapshot was taken.</summary>
   public IsolationLevel IsolationLevel { get; } = transaction.IsolationLevel;

   internal static TransactionSnapshot? TakeSnapshot()
   {
      var currentTransaction = Transaction.Current;
      return currentTransaction == null ? null : new TransactionSnapshot(currentTransaction);
   }
}
