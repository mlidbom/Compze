using System.Transactions;

namespace Compze.Utilities.SystemCE.ThreadingCE;

public class SingleTransactionUsageGuard : IUsageGuard
{
   Transaction? _transaction = Transaction.Current;
   readonly object _guarded;
   public SingleTransactionUsageGuard(object guarded) => _guarded = guarded;

   public void EnsureAccessValid()
   {
      _transaction ??= Transaction.Current;
      if(Transaction.Current != null && Transaction.Current != _transaction)
      {
         throw new ComponentUsedByMultipleTransactionsException(_guarded.GetType());
      }
   }
}
