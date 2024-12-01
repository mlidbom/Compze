using System;
using System.Transactions;

namespace Composable.Testing.Threading;

class TransactionSnapshot(Transaction transaction)
{
   public class TransactionInformationSnapshot(TransactionInformation information)
   {
      public string LocalIdentifier { get; } = information.LocalIdentifier;
      public Guid DistributedIdentifier { get; } = information.DistributedIdentifier;
      public TransactionStatus Status { get; } = information.Status;
   }

   public IsolationLevel IsolationLevel { get; } = transaction.IsolationLevel;

   public TransactionInformationSnapshot TransactionInformation { get; } = new(transaction.TransactionInformation);

   public static TransactionSnapshot? TakeSnapshot()
   {
      var currentTransaction = Transaction.Current;
      return currentTransaction == null ? null : new TransactionSnapshot(currentTransaction);
   }
}