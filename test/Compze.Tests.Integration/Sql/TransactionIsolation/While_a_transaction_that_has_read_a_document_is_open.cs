using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Must;
using Compze.Tests.Common;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitMatrix;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Sql.TransactionIsolation;

///<summary>Specifies the isolation level each sql layer runs Compze's transactions at, as a consumer sees it. Every transaction Compze<br/>
/// opens, through <see cref="TransactionScopeCe"/>, asks for read committed: a read in it never finds another transaction's<br/>
/// uncommitted change, and does find a change another transaction committed after its earlier reads, which repeatable read,<br/>
/// snapshot and serializable would not.</summary>
///<remarks>A provider can run the transaction at another level without anything else failing: MySqlConnector's XA transactions,<br/>
/// which Compze used until <c>CompzeMySqlConnectionString</c> turned them off, ran at the server's default, repeatable read.</remarks>
///<remarks>SQLite has no read committed. Microsoft.Data.Sqlite begins a transaction asked for read committed as a serializable one, with<br/>
/// <c>BEGIN IMMEDIATE</c>, which takes the database's write lock at once, and Compze's SQLite connection holds a write gate of its own<br/>
/// for the whole transaction: transactions on one SQLite database run one at a time. So on SQLite, another unit of work cannot delete<br/>
/// the document while the transaction is open, and its deletion waits instead, which<br/>
/// <see cref="and_another_unit_of_work_tries_to_delete_the_document_on_sqlite"/> specifies.</remarks>
public class While_a_transaction_that_has_read_a_document_is_open : DocumentDbTestsBase
{
   const string SqliteRunsOneTransactionAtATime = "On SQLite, transactions on one database run one at a time, so another unit of work cannot delete the document while the transaction is open: and_another_unit_of_work_tries_to_delete_the_document_on_sqlite specifies what happens instead.";
   const string TheOtherSqlLayersRunReadCommitted = "The other sql layers run the transaction at read committed, which the other contexts specify.";

   ///<summary>How long the specifications wait for the other unit of work, or for the transaction, before they fail: far longer than either takes.</summary>
   static readonly TimeSpan OtherUnitOfWorkTimeout = 20.Seconds();

   readonly User _document = new();

   protected While_a_transaction_that_has_read_a_document_is_open() => UseInTransactionalScope((_, updater) => updater.Save(_document));

   ///<summary>Reads the document in a scope of its own, so that the database answers, not a session's cache of an earlier read: inside a<br/>
   /// transaction, the read joins it. Returns null when the document is not found.</summary>
   User? ReadTheDocument() => Container.ExecuteInIsolatedScope(scope => scope.DocumentDbReader().TryGet(_document.Id, out User? found) ? found : null);

   ///<summary>Starts deleting the document in a unit of work of its own, on a thread of its own, outside the transaction the specification<br/>
   /// has open. Runs <paramref name="beforeCommitting"/> once the deletion is made, before the unit of work commits.</summary>
   Task DeleteTheDocumentInAnotherUnitOfWork(TestingTaskRunner runner, Action? beforeCommitting = null) =>
      runner.RunAsync(() => TransactionScopeCe.SuppressAmbient(() => Container.ExecuteUnitOfWork(unitOfWork =>
      {
         unitOfWork.DocumentDbUpdater().Delete(_document);
         beforeCommitting?.Invoke();
      })));

   public class and_another_unit_of_work_has_deleted_the_document_but_not_committed_yet : While_a_transaction_that_has_read_a_document_is_open
   {
      User? _readWhileTheDeletionIsUncommitted;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         var deletedButNotCommittedGate = IThreadGate.NewClosed(new WaitTimeout(OtherUnitOfWorkTimeout));
         await using var otherUnitOfWork = new TestingTaskRunner(OtherUnitOfWorkTimeout);

         await TransactionScopeCe.ExecuteAsync(async () =>
         {
            ReadTheDocument();
            _ = DeleteTheDocumentInAnotherUnitOfWork(otherUnitOfWork, beforeCommitting: () => deletedButNotCommittedGate.AwaitPassThrough());
            await deletedButNotCommittedGate.AwaitQueueLengthEqualToAsync(1);
            _readWhileTheDeletionIsUncommitted = ReadTheDocument();
            deletedButNotCommittedGate.Open();
         });
      }

      [PCT, Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], SqliteRunsOneTransactionAtATime)]
      public void the_transaction_still_finds_the_document() => _readWhileTheDeletionIsUncommitted.Must().NotBeNull();
   }

   public class and_another_unit_of_work_has_deleted_the_document_and_committed : While_a_transaction_that_has_read_a_document_is_open
   {
      User? _readAfterTheDeletionCommitted;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await using var otherUnitOfWork = new TestingTaskRunner(OtherUnitOfWorkTimeout);

         await TransactionScopeCe.ExecuteAsync(async () =>
         {
            ReadTheDocument();
            //Times out where the transaction's read holds a lock the deletion waits for, as repeatable read and serializable do on SQL Server.
            await DeleteTheDocumentInAnotherUnitOfWork(otherUnitOfWork).WaitAsync(OtherUnitOfWorkTimeout);
            _readAfterTheDeletionCommitted = ReadTheDocument();
         });
      }

      [PCT, Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], SqliteRunsOneTransactionAtATime)]
      public void the_transaction_no_longer_finds_the_document() => _readAfterTheDeletionCommitted.Must().BeNull();
   }

   public class and_another_unit_of_work_tries_to_delete_the_document_on_sqlite : While_a_transaction_that_has_read_a_document_is_open
   {
      ///<summary>How long the transaction gives the deletion to commit before it reads the document again: on SQLite the deletion waits for<br/>
      /// the transaction, so the whole of it passes, and a deletion that did not wait would commit in a fraction of it.</summary>
      static readonly TimeSpan TimeTheDeletionIsGivenToCommit = 1.Seconds();

      bool _theDeletionCommittedWhileTheTransactionWasOpen;
      User? _readAfterTheDeletionWasGivenTimeToCommit;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         //Disposing it waits for the deletion to commit, once the transaction has completed.
         await using var otherUnitOfWork = new TestingTaskRunner(OtherUnitOfWorkTimeout);

         await TransactionScopeCe.ExecuteAsync(async () =>
         {
            ReadTheDocument();
            var deletion = DeleteTheDocumentInAnotherUnitOfWork(otherUnitOfWork);
            //The time passing is what this context expects, so its TimeoutException is suppressed.
            await deletion.WaitAsync(TimeTheDeletionIsGivenToCommit).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            _theDeletionCommittedWhileTheTransactionWasOpen = deletion.IsCompleted;
            _readAfterTheDeletionWasGivenTimeToCommit = ReadTheDocument();
         });
      }

      [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], TheOtherSqlLayersRunReadCommitted)]
      public void the_deletion_waits_until_the_transaction_has_completed_as_transactions_on_one_sqlite_database_run_one_at_a_time() =>
         _theDeletionCommittedWhileTheTransactionWasOpen.Must().BeFalse();

      [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], TheOtherSqlLayersRunReadCommitted)]
      public void the_transaction_still_finds_the_document() => _readAfterTheDeletionWasGivenTimeToCommit.Must().NotBeNull();
   }
}
