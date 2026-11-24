using System.Transactions;
using Compze.Sql.Common._internal;
using Microsoft.Data.Sqlite;
using IsolationLevel = System.Data.IsolationLevel;

namespace Compze.Sql.Sqlite._internal;

partial interface ICompzeSqliteConnection
{
   public sealed partial class CompzeSqliteConnection
   {
      ///<summary>The transaction a SQLite connection begins to take part in an ambient transaction, as that transaction's decision<br/>
      /// (<see cref="DatabaseTransactionAsTheDecision"/>), and the database's write gate, which it holds from before it begins until it<br/>
      /// has committed or rolled back: taken before the transaction can take any lock.</summary>
      ///<remarks>It begins with BEGIN IMMEDIATE, which <see cref="SqliteConnection.BeginTransaction(IsolationLevel)"/> issues at read<br/>
      /// committed, so it holds the database's write lock from its start, across processes too.</remarks>
      sealed class OwnTransaction : IDisposable
      {
         readonly SemaphoreSlim _writeGate;

         OwnTransaction(SqliteTransaction databaseTransaction, SemaphoreSlim writeGate, Transaction ambientTransaction)
         {
            DatabaseTransaction = databaseTransaction;
            _writeGate = writeGate;
            AmbientTransaction = ambientTransaction;
         }

         ///<summary>Takes <paramref name="writeGate"/>, begins a transaction on <paramref name="connection"/>, and has it take part in<br/>
         /// <paramref name="ambientTransaction"/> as its decision.</summary>
         internal static OwnTransaction Begin(SqliteConnection connection, SemaphoreSlim writeGate, Transaction ambientTransaction)
         {
            //urgent: this waits for the write gate with no timeout, and nothing records who holds it. A transaction that hangs, in one
            //   endpoint of a process, blocks every other transaction on the same database in that process forever, and nothing says which
            //   one holds it: across processes SQLite's own busy_timeout gives up after 5 s, inside one process nothing does. Give it a
            //   timeout whose exception names the holder: its transaction, its thread, where and when it took the gate. The gate is held
            //   for a whole transaction and released on whatever thread completes it, so it needs the Compze.Threading lock owned by a
            //   transaction that TaggregateUpdateLocks' todo asks for (Magnus, 2026-11-24).
            writeGate.Wait();
            SqliteTransaction databaseTransaction;
            try
            {
               databaseTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            }
            catch
            {
               writeGate.Release();
               throw;
            }

            var ownTransaction = new OwnTransaction(databaseTransaction, writeGate, ambientTransaction);
            try
            {
               //A failed commit leaves a SQLite transaction uncommitted: a local database has no connection to break mid-answer.
               DatabaseTransactionAsTheDecision.TakePartIn(ambientTransaction, "a SQLite connection", ownTransaction.Commit, ownTransaction.RollBack, isAFailureThatLeftItUncommitted: _ => true);
            }
            catch
            {
               ownTransaction.Dispose();
               throw;
            }

            return ownTransaction;
         }

         internal SqliteTransaction DatabaseTransaction { get; }

         ///<summary>The ambient transaction this takes part in.</summary>
         internal Transaction AmbientTransaction { get; }

         ///<summary>Whether this transaction has committed, rolled back, or been disposed, and released the write gate.</summary>
         internal bool HasEnded { get; private set; }

         void Commit()
         {
            try
            {
               DatabaseTransaction.Commit();
            }
            finally
            {
               Dispose();
            }
         }

         void RollBack()
         {
            try
            {
               DatabaseTransaction.Rollback();
            }
            finally
            {
               Dispose();
            }
         }

         ///<summary>Ends the transaction, rolling it back if it neither committed nor rolled back, and releases the write gate.</summary>
         public void Dispose()
         {
            if(HasEnded) return;
            HasEnded = true;
            try
            {
               DatabaseTransaction.Dispose();
            }
            finally
            {
               _writeGate.Release();
            }
         }
      }
   }
}
