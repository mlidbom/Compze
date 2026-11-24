using System.Transactions;

namespace Compze.Sql.InternalSpecifications.TransactionCommit;

///<summary>A participant in a transaction, as a consumer's own would be, that records what the transaction tells it of its outcome,<br/>
/// and, when told the transaction committed, what the database held then, read outside the transaction.</summary>
sealed class ParticipantThatRecordsWhatItIsTold : IEnlistmentNotification
{
   readonly Func<long> _rowsTheDatabaseHolds;

   ///<summary><paramref name="rowsTheDatabaseHolds"/> reads, outside the transaction, how many rows the database holds of those the<br/>
   /// transaction writes.</summary>
   internal ParticipantThatRecordsWhatItIsTold(Func<long> rowsTheDatabaseHolds) => _rowsTheDatabaseHolds = rowsTheDatabaseHolds;

   ///<summary>The outcome the transaction told this participant of, or null if it told none.</summary>
   internal TransactionStatus? ToldOutcome { get; private set; }

   ///<summary>How many rows the database held, read outside the transaction, when the transaction told this participant it committed.</summary>
   internal long? RowsTheDatabaseHeldWhenToldItCommitted { get; private set; }

   public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

   public void Commit(Enlistment enlistment)
   {
      RowsTheDatabaseHeldWhenToldItCommitted = _rowsTheDatabaseHolds();
      Told(TransactionStatus.Committed, enlistment);
   }

   public void Rollback(Enlistment enlistment) => Told(TransactionStatus.Aborted, enlistment);

   public void InDoubt(Enlistment enlistment) => Told(TransactionStatus.InDoubt, enlistment);

   void Told(TransactionStatus outcome, Enlistment enlistment)
   {
      ToldOutcome = outcome;
      enlistment.Done();
   }
}
