using System;
using System.Transactions;
using Compze.Contracts;

namespace Compze.SystemCE.TransactionsCE;

static class TransactionCE
{
   internal static void OnCommittedSuccessfully(this Transaction @this, Action action)
   {
      @this.TransactionCompleted += (_, args) =>
      {
         Assert.Argument.NotNull(args.Transaction);
         if(args.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
         {
            action();
         }
      };
   }

   internal static void OnCompleted(this Transaction @this, Action action) => @this.TransactionCompleted += (_, _) => action();

   internal static IDisposable NoTransactionEscalationScope(string scopeDescription)
   {
      var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier ?? Guid.Empty;

      return DisposableCE.Create(() =>
      {
         if(Transaction.Current != null && transactionInformationDistributedIdentifierBefore == Guid.Empty && Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
         {
            throw new Exception($"{scopeDescription} escalated transaction to distributed. For now this is disallowed");
         }
      });
   }
}