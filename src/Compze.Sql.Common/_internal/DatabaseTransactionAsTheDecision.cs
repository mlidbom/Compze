using System.Diagnostics;
using System.Transactions;

namespace Compze.Sql.Common._internal;

///<summary>The database transaction of one of Compze's connections, taking part in the ambient <see cref="Transaction"/> as its<br/>
/// decision: it commits in a single phase, once every other participant has prepared, and its commit decides whether the<br/>
/// transaction committed. So every participant told the outcome, such as a unit of work installing taggregates in memory, is<br/>
/// told only once the database has committed.</summary>
///<remarks>Why: a participant that commits only when it is told the outcome, as MySqlConnector's own enlistment without XA does,<br/>
/// commits after the participants told before it, which have then acted on a commit that hasn't happened. And a failure of<br/>
/// that commit throws out of the transaction's second phase: the participants after it are never told, the transaction never<br/>
/// completes, and whatever waits for its completion, such as the locks of a unit of work, waits for ever.<br/>
/// SQL Server's connection takes part as its transaction's durable participant, and PostgreSQL's as a participant that commits in<br/>
/// a single phase: both already commit as the decision.</remarks>
///<remarks>A database that can't prepare a commit can't take part in a two-phase commit, which is what a transaction holding one<br/>
/// more participant that can't commit in a single phase makes. Asked to prepare, this rolls back, and votes for the transaction<br/>
/// to roll back too: such a transaction is refused, rather than committed one participant after the other with no way to undo<br/>
/// the first if the second fails. Compze's own participants never make a two-phase commit.</remarks>
sealed class DatabaseTransactionAsTheDecision : ISinglePhaseNotification
{
   readonly string _description;
   readonly Action _commit;
   readonly Action _rollBack;
   readonly Func<Exception, bool> _isAFailureThatLeftItUncommitted;

   DatabaseTransactionAsTheDecision(string description, Action commit, Action rollBack, Func<Exception, bool> isAFailureThatLeftItUncommitted)
   {
      _description = description;
      _commit = commit;
      _rollBack = rollBack;
      _isAFailureThatLeftItUncommitted = isAFailureThatLeftItUncommitted;
   }

   ///<summary>Has a database transaction, just begun, take part in <paramref name="transaction"/> as its decision.<br/>
   /// <paramref name="description"/> is what the database is, as a message names it, such as "a SQLite connection".<br/>
   /// <paramref name="commit"/> and <paramref name="rollBack"/> end the database transaction, and release whatever it holds, such<br/>
   /// as a gate, whether they succeed or fail. <paramref name="isAFailureThatLeftItUncommitted"/> tells, of an exception<br/>
   /// <paramref name="commit"/> threw, whether the database certainly didn't commit, such as an error the server answered with,<br/>
   /// as opposed to a connection that broke before the answer came, which leaves the transaction in doubt.</summary>
   internal static void TakePartIn(Transaction transaction, string description, Action commit, Action rollBack, Func<Exception, bool> isAFailureThatLeftItUncommitted) =>
      transaction.EnlistVolatile(new DatabaseTransactionAsTheDecision(description, commit, rollBack, isAFailureThatLeftItUncommitted), EnlistmentOptions.None);

   ///<summary>The transaction's decision: the database commits, and the transaction with it. A commit that fails aborts the<br/>
   /// transaction, or, when the database may have committed, leaves it in doubt.</summary>
   public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
   {
      try
      {
         _commit();
      }
#pragma warning disable CA1031 //A failed commit is the transaction's outcome: reported through the enlistment, it comes out of the transaction's commit.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         if(_isAFailureThatLeftItUncommitted(exception)) singlePhaseEnlistment.Aborted(exception);
         else singlePhaseEnlistment.InDoubt(exception);
         return;
      }

      singlePhaseEnlistment.Committed();
   }

   ///<summary>Asked only in a two-phase commit, which this refuses: see the remarks on <see cref="DatabaseTransactionAsTheDecision"/>.</summary>
   public void Prepare(PreparingEnlistment preparingEnlistment)
   {
      _rollBack();
      preparingEnlistment.ForceRollback(new InvalidOperationException(
                                           $"This transaction holds {_description} and another participant that takes part in its phase 1 or is durable, so it would commit in two phases. "
                                         + $"{_description} can't prepare a commit: it commits only as its transaction's decision, in a single phase. In two phases, the database and the other participant "
                                         + "would commit one after the other, and a crash or a failure between the two would leave one committed and the other not. So the transaction is refused, and rolls back. "
                                         + "Have the other participant take part at the transaction's phase 0 (EnlistmentOptions.EnlistDuringPrepareRequired), where it prepares before the database commits "
                                         + "and is told the outcome after, or keep the two in separate transactions."));
   }

   public void Commit(Enlistment enlistment) => throw new UnreachableException($"{_description} was told to commit the second phase of a two-phase commit, and it votes to roll back every one.");

   public void Rollback(Enlistment enlistment)
   {
      _rollBack();
      enlistment.Done();
   }

   ///<summary>Told only when another participant left the transaction in doubt, which can't happen: this is the transaction's decision.</summary>
   public void InDoubt(Enlistment enlistment) => throw new UnreachableException($"{_description} was told its transaction is in doubt, and it is that transaction's decision.");
}
