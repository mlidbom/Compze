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

   static volatile bool _distributedTransactionsAllowed;

   ///<summary>Permits transactions to escalate to distributed (MSDTC) transactions process-wide, opting out of the loud<br/>
   /// <see cref="NoTransactionEscalationScope"/> assertion that otherwise treats any escalation as a bug.</summary>
   ///<remarks>Escalation - a second durable resource enlisting in one transaction - is almost always an accident here, where each<br/>
   /// transaction uses exactly one connection, so it is asserted against by default. An application that genuinely needs<br/>
   /// distributed transactions calls this once at startup, before any transaction is created. It also enables the platform's<br/>
   /// <see cref="TransactionManager.ImplicitDistributedTransactions"/>, without which escalation throws at the runtime level<br/>
   /// regardless (.NET 7+); that in turn requires a distributed transaction coordinator (MSDTC, Windows-only), so distributed<br/>
   /// transactions remain unavailable on other platforms.</remarks>
   public static void AllowDistributedTransactions()
   {
      _distributedTransactionsAllowed = true;
      TransactionManager.ImplicitDistributedTransactions = true;
   }

   ///<summary>Asserts that the work done within the returned scope does not escalate the ambient <see cref="Transaction.Current"/><br/>
   /// to a distributed transaction, failing loud with a <see cref="TransactionEscalatedToDistributedException"/> if it does.<br/>
   /// A no-op when there is no ambient transaction, or when distributed transactions have been permitted process-wide via<br/>
   /// <see cref="AllowDistributedTransactions"/>.</summary>
   ///<remarks>Escalation happens when a second durable resource enlists in a transaction that began with one. The<br/>
   /// one-connection-per-transaction model (see <c>DbConnectionPool</c>) must never do this, so opening a connection is wrapped<br/>
   /// in this scope: a violation is a bug to fix, surfaced at the exact operation that caused it rather than as the runtime's<br/>
   /// generic distributed-transaction error. Detected by <see cref="TransactionInformation.DistributedIdentifier"/> going from<br/>
   /// empty to assigned across the scope - the transaction manager stamps it the moment a transaction becomes distributed.</remarks>
   public static IDisposable NoTransactionEscalationScope(string scopeDescription)
   {
      if(_distributedTransactionsAllowed) return new Disposable(() => { });

      var distributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier ?? Guid.Empty;
      return new Disposable(() =>
      {
         if(Transaction.Current != null
         && distributedIdentifierBefore == Guid.Empty
         && Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
            throw new TransactionEscalatedToDistributedException(scopeDescription);
      });
   }
}
