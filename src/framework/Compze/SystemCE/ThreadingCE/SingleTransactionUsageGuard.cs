using System.Transactions;

namespace Compze.SystemCE.ThreadingCE;

class SingleTransactionUsageGuard : ISingleContextUseGuard
{
   Transaction? _transaction = Transaction.Current;

   public void AssertNoContextChangeOccurred(object guarded)
   {
      _transaction ??= Transaction.Current;
      if(Transaction.Current != null && Transaction.Current != _transaction)
      {
         throw new ComponentUsedByMultipleTransactionsException(guarded.GetType());
      }
   }
}