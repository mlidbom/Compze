using System.Transactions;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.TransactionsCE;

public static class TransactionCE
{
   public static void OnCommittedSuccessfully(this Transaction @this, Action action)
   {
      @this.TransactionCompleted += (_, args) =>
      {
         Argument.NotNull(args.Transaction);
         if(args.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
         {
            action();
         }
      };
   }

   public static void OnCompleted(this Transaction @this, Action action) => @this.TransactionCompleted += (_, _) => action();

   public static IDisposable NoTransactionEscalationScope(string scopeDescription)
   {
      var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier ?? Guid.Empty;

      return new Disposable(() =>
      {
         if(Transaction.Current != null && transactionInformationDistributedIdentifierBefore == Guid.Empty && Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
         {
            throw new Exception($"{scopeDescription} escalated transaction to distributed. For now this is disallowed");
         }
      });
   }
}