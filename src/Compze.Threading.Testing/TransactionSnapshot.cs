using System.Transactions;

namespace Compze.Threading.Testing;

public class TransactionSnapshot(Transaction transaction)
{
   public IsolationLevel IsolationLevel { get; } = transaction.IsolationLevel;

   internal static TransactionSnapshot? TakeSnapshot()
   {
      var currentTransaction = Transaction.Current;
      return currentTransaction == null ? null : new TransactionSnapshot(currentTransaction);
   }
}
