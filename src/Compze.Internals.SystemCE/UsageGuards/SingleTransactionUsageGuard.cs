using System.Transactions;

namespace Compze.Internals.SystemCE.UsageGuards;

public class SingleTransactionUsageGuard(object guarded) : IUsageGuard
{
   Transaction? _transaction = Transaction.Current;
   readonly object _guarded = guarded;

   public void EnsureAccessValid()
   {
      Interlocked.CompareExchange(ref _transaction, Transaction.Current, null);
      if(Transaction.Current != null && Transaction.Current != _transaction)
      {
         throw new ComponentUsedByMultipleTransactionsException(_guarded.GetType());
      }
   }
}
