using System.Transactions;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.TransactionsCE;

public static class TransactionCE
{
   extension(Transaction @this)
   {
      public void OnCommittedSuccessfully(Action action)
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

      ///<summary>Runs <paramref name="action"/> when the transaction completes any way but a successful commit — rollback or<br/>
      /// in-doubt. The mirror of <see cref="OnCommittedSuccessfully"/>: registering with both covers every outcome exactly once.</summary>
      public void OnCompletedWithoutCommitting(Action action)
      {
         @this.TransactionCompleted += (_, args) =>
         {
            Argument.NotNull(args.Transaction);
            if(args.Transaction.TransactionInformation.Status != TransactionStatus.Committed)
            {
               action();
            }
         };
      }

      public void OnCompleted(Action action) => @this.TransactionCompleted += (_, _) => action();
   }

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