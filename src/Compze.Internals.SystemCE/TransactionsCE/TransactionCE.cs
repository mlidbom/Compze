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
}
