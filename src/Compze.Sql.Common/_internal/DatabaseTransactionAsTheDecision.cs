using System.Diagnostics;
using System.Transactions;

namespace Compze.Sql.Common._internal;

///<summary>A transaction one of Compze's connections begins in its database to take part in the ambient <see cref="Transaction"/>, as<br/>
/// that transaction's decision: the database commits once every other participant has prepared, before any is told the outcome, and<br/>
/// whether it commits decides whether the transaction did. So a participant told that the transaction committed, such as a unit of<br/>
/// work, is told only once the database has.</summary>
///<remarks>Why: a database transaction that commits only when it is told the outcome, as Compze's SQLite connection's did, and as<br/>
/// MySqlConnector's own enlistment does, commits after the participants told before it, which have then acted on a commit that<br/>
/// hadn't happened. And when that commit fails, the failure throws out of the transaction's notifications: the participants after<br/>
/// it are never told, the transaction never completes, and what waits for its completion, such as the connection pool's connection<br/>
/// for the transaction and a unit of work's locks, waits for ever. SQL Server's and PostgreSQL's connections commit as the decision<br/>
/// by themselves.</remarks>
///<remarks>It takes part as the transaction's durable participant. .NET asks that to commit in a single phase, as the decision, once<br/>
/// every volatile participant has prepared, however many there are, and tells each of them the outcome after. A transaction has one<br/>
/// durable participant: a second, such as another database's connection, makes it distributed, which .NET refuses unless distributed<br/>
/// transactions are allowed. A distributed transaction commits in two phases, and asks this transaction to prepare, which it refuses:<br/>
/// see <see cref="Prepare"/>.<br/>
/// Disposing it ends it: a database transaction that hasn't committed rolls back as it is disposed, in both of the providers Compze<br/>
/// uses this for.</remarks>
abstract class DatabaseTransactionAsTheDecision : ISinglePhaseNotification, IDisposable
{
   ///<summary>Identifies Compze's database transactions as a resource manager to .NET's transactions. Only a recovery would use it, and there is none.</summary>
   static readonly Guid ResourceManagerIdentifier = new("b7fd27a4-db7c-489b-8045-320c6fb17a08");

   readonly DbTransaction _databaseTransaction;
   readonly string _connectionDescription;

   ///<summary>Takes <paramref name="databaseTransaction"/>, just begun, to take part in <paramref name="ambientTransaction"/>, which the<br/>
   /// subclass's constructor has it do, once it has set what <see cref="OnEnded"/> releases: <see cref="TakePartInTheAmbientTransaction"/>.<br/>
   /// <paramref name="connectionDescription"/> is the connection, as a message names it, such as "a SQLite connection".</summary>
   protected DatabaseTransactionAsTheDecision(DbTransaction databaseTransaction, Transaction ambientTransaction, string connectionDescription)
   {
      _databaseTransaction = databaseTransaction;
      AmbientTransaction = ambientTransaction;
      _connectionDescription = connectionDescription;
   }

   ///<summary>The transaction this database transaction takes part in.</summary>
   internal Transaction AmbientTransaction { get; }

   ///<summary>Whether this database transaction has committed or rolled back, and released what it held.</summary>
   internal bool HasEnded { get; private set; }

   ///<summary>Has <paramref name="command"/> run in this database transaction.</summary>
   internal void Includes(DbCommand command) => command.Transaction = _databaseTransaction;

   ///<summary>Has this database transaction take part in <see cref="AmbientTransaction"/> as its decision, or, if the ambient transaction<br/>
   /// refuses it, ends it, and throws what refused it.</summary>
   protected void TakePartInTheAmbientTransaction()
   {
      try
      {
         AmbientTransaction.EnlistDurable(ResourceManagerIdentifier, this, EnlistmentOptions.None);
      }
      catch
      {
         Dispose();
         throw;
      }
   }

   ///<summary>Whether the database certainly didn't commit, after its commit threw <paramref name="commitFailure"/>: an error the database<br/>
   /// answered the commit with, as opposed to a connection that broke before the answer came, which leaves unknown whether it committed.</summary>
   protected abstract bool CertainlyDidNotCommitAfter(Exception commitFailure);

   ///<summary>Releases what the database transaction held besides its connection, once it has committed or rolled back.</summary>
   protected virtual void OnEnded() {}

   ///<summary>Whether <paramref name="rollbackFailure"/>, thrown as the database transaction was disposed, and so rolled back, means only<br/>
   /// that the database had already rolled it back itself, as a failed commit can make it do, so there was nothing to roll back.</summary>
   protected virtual bool TheDatabaseHadRolledBackItselfWhen(Exception rollbackFailure) => false;

   ///<summary>The ambient transaction's decision: the database commits, and the transaction with it. A commit that fails aborts the<br/>
   /// transaction, or, when the database may have committed, leaves it in doubt.</summary>
   ///<remarks>It ends, releasing what it holds, before it answers: the answer has .NET tell the other participants the outcome, and<br/>
   /// complete the transaction, and what runs then may begin the next transaction on the same database.</remarks>
   public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
   {
      try
      {
         _databaseTransaction.Commit();
      }
#pragma warning disable CA1031 //A failed commit is the transaction's outcome: answered through the enlistment, it comes out of completing the transaction, as the cause of its abort or its doubt.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         Dispose();
         if(CertainlyDidNotCommitAfter(exception)) singlePhaseEnlistment.Aborted(exception);
         else singlePhaseEnlistment.InDoubt(exception);
         return;
      }

      Dispose();
      singlePhaseEnlistment.Committed();
   }

   ///<summary>The ambient transaction is distributed, and asks its participants to prepare a two-phase commit. The database can't<br/>
   /// prepare a commit, so this rolls back, and has the transaction roll back.</summary>
   public void Prepare(PreparingEnlistment preparingEnlistment)
   {
      Dispose();
      preparingEnlistment.ForceRollback(new InvalidOperationException(
                                           $"This transaction holds {_connectionDescription}, and became distributed: it holds another durable resource too, such as another database's connection, so it commits in two phases. "
                                         + $"{_connectionDescription} commits only as its transaction's decision, in a single phase: its database can't prepare a commit for a second phase. So the transaction rolls back. "
                                         + "Keep each database, and each other durable resource, in transactions of its own."));
   }

   ///<summary>Told only in the second phase of a two-phase commit, which never comes: this votes to roll back every one, in <see cref="Prepare"/>.</summary>
   public void Commit(Enlistment enlistment) =>
      throw new UnreachableException($"{_connectionDescription} was told to commit the second phase of a two-phase commit, and it votes to roll back every one.");

   public void Rollback(Enlistment enlistment)
   {
      Dispose();
      enlistment.Done();
   }

   ///<summary>Told only in a two-phase commit, which never comes this far: this votes to roll back every one, in <see cref="Prepare"/>.</summary>
   public void InDoubt(Enlistment enlistment) =>
      throw new UnreachableException($"{_connectionDescription} was told that its transaction is in doubt, in a two-phase commit, and it votes to roll back every one.");

   ///<summary>Ends the database transaction, rolling it back unless it has committed, and releases what it held. Does nothing once it has ended.</summary>
   public void Dispose()
   {
      if(HasEnded) return;
      HasEnded = true;
      try
      {
         _databaseTransaction.Dispose();
      }
      catch(Exception exception) when(TheDatabaseHadRolledBackItselfWhen(exception))
      {
         //Nothing to roll back: the database transaction has ended.
      }
      finally
      {
         OnEnded();
      }
   }
}
