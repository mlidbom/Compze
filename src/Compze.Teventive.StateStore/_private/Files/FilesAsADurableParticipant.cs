using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>A state store's files taking part in a unit of work's transaction as its one durable participant: the write commits in a<br/>
/// single phase, as the transaction's decision, before the unit of work and any volatile participant commit. It is how the store keeps<br/>
/// its files out of a transaction that also writes a database, which it can't commit as one with them.</summary>
///<remarks>A transaction has at most one durable participant unless it becomes distributed, which .NET refuses by default, so a SQL<br/>
/// Server connection, also durable, is refused by .NET itself. Database connections that enlist as volatile participants, as<br/>
/// PostgreSQL's, MySQL's and Compze's SQLite connections do, are refused by <see cref="DurableResourcesOfTransactions"/> when they are<br/>
/// Compze's.<br/>
/// The files never commit as part of a distributed transaction: they refuse a transaction that is distributed when they join it, and<br/>
/// vote to roll back if one asks them to prepare, its two-phase commit. There is no recovery: the journal finishes or discards an<br/>
/// interrupted commit at the next start, and nothing is reenlisted.</remarks>
sealed class FilesAsADurableParticipant : ISinglePhaseNotification
{
   ///<summary>Identifies the state store's files as a resource manager to the transaction. Only a recovery would use it, and there is none.</summary>
   static readonly Guid ResourceManagerIdentifier = new("4a1c2a3e-5e5b-4d0f-9a0e-9c4b3b8e6f21");

   readonly PreparedFileWrite _write;

   FilesAsADurableParticipant(PreparedFileWrite write) => _write = write;

   ///<summary>Has <paramref name="write"/>, the files of the state store in <paramref name="storesFolder"/>, take part in<br/>
   /// <paramref name="transaction"/> as its one durable participant, committing as its decision. Refused if the transaction is<br/>
   /// distributed, or if a database connection of Compze's takes part in it.</summary>
   internal static void TakePartIn(Transaction transaction, PreparedFileWrite write, string storesFolder)
   {
      State.Assert(transaction.TransactionInformation.DistributedIdentifier == Guid.Empty,
                   () => $"A state store kept in files, in {storesFolder}, can't take part in a distributed transaction: it commits only as a transaction's one durable participant, in a single phase, and has no recovery.");
      DurableResourcesOfTransactions.TakesPart(transaction, DurableResource.ThatCantShareItsTransaction($"a state store kept in files, in {storesFolder}", other => WhyTheFilesCantShareTheirTransactionWith(other, storesFolder)));
      transaction.EnlistDurable(ResourceManagerIdentifier, new FilesAsADurableParticipant(write), EnlistmentOptions.None);
      write.CommitsAsTheTransactionsDecision();
   }

   static string WhyTheFilesCantShareTheirTransactionWith(DurableResource other, string storesFolder) =>
      $"This transaction holds a state store kept in files, in {storesFolder}, and {other.Description}. A crash between their commits would lose the files' change, "
    + "or, in an endpoint with exactly-once delivery, deliver a tevent about a change that was lost, or count a tessage as handled while its change was lost. "
    + "Compze can't make them commit as one. Keep them in separate units of work, or, if that loss is acceptable, declare it on the store's files: "
    + $"files => files.{nameof(TaggregateFiles.AllowedInTransactionsThatAlsoWriteADatabase)}().";

   ///<summary>The transaction's decision, made by the files as its one durable participant: the write commits, or, if it fails before its<br/>
   /// journal is written, the transaction rolls back with the failure.</summary>
   public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
   {
      try
      {
         _write.CommitAsTheTransactionsDecision();
      }
#pragma warning disable CA1031 //A failure of the transaction's decision is surfaced by aborting the transaction with it: the consumer gets it as the rollback's cause.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         singlePhaseEnlistment.Aborted(exception);
         return;
      }
      singlePhaseEnlistment.Committed();
   }

   ///<summary>The transaction asks the files to prepare: a two-phase commit, which only a distributed transaction makes. The files vote to<br/>
   /// roll it back, having no recovery to make a two-phase commit safe.</summary>
   public void Prepare(PreparingEnlistment preparingEnlistment)
   {
      _write.Discard();
      preparingEnlistment.ForceRollback(new InvalidOperationException("A state store kept in files commits only as a transaction's one durable participant, in a single phase. It can't take part in a distributed transaction, having no recovery."));
   }

   ///<summary>Only a two-phase commit commits here, and the files vote to roll back every one in <see cref="Prepare"/>.</summary>
   public void Commit(Enlistment enlistment) => enlistment.Done();

   public void Rollback(Enlistment enlistment)
   {
      _write.Discard();
      enlistment.Done();
   }

   public void InDoubt(Enlistment enlistment) => enlistment.Done();
}
