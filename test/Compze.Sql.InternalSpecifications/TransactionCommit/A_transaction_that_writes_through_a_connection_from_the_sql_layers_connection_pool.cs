using System.Globalization;
using System.Transactions;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Sql.Common._internal;
using Compze.Sql.Sqlite._internal;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using SQLitePCL;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Sql.InternalSpecifications.TransactionCommit;

///<summary>How a transaction that writes through a connection from the sql layer's connection pool commits: the database commits as<br/>
/// the transaction's decision, once every other participant has prepared, and before any is told the outcome. So a participant told<br/>
/// the transaction committed, as Compze's units of work are, acts only on what the database has committed, and a commit the database<br/>
/// fails ends the transaction as aborted or in doubt, telling every participant, and completing it.</summary>
public class A_transaction_that_writes_through_a_connection_from_the_sql_layers_connection_pool : UniversalTestBase
{
   const string DomainDatabaseName = "TransactionCommitDatabase";

   readonly DomainDatabaseOnTheCurrentSqlLayer _database = DomainDatabaseOnTheCurrentSqlLayer.Named(DomainDatabaseName);

   public A_transaction_that_writes_through_a_connection_from_the_sql_layers_connection_pool() =>
      _database.ExecuteNonQuery(TestEnv.SqlLayer == SqlLayer.PgSql
                                   ? "CREATE TABLE Written (Id INT NOT NULL PRIMARY KEY); CREATE TABLE Parent (Id INT NOT NULL PRIMARY KEY); CREATE TABLE ChildOfNoParent (ParentId INT NOT NULL REFERENCES Parent (Id) DEFERRABLE INITIALLY DEFERRED);"
                                   : "CREATE TABLE Written (Id INT NOT NULL PRIMARY KEY)");

   protected override async Task DisposeAsyncInternal() => await _database.DisposeAsync();

   void WriteARow() => _database.ExecuteNonQuery("INSERT INTO Written (Id) VALUES (1)");

   ///<summary>How many rows the table the transaction writes holds, read outside any transaction.</summary>
   long RowsWritten() => Convert.ToInt64(TransactionScopeCe.Execute(() => _database.ExecuteScalar("SELECT COUNT(*) FROM Written"), TransactionScopeOption.Suppress), CultureInfo.InvariantCulture);

   ParticipantThatRecordsWhatItIsTold EnlistAParticipant(EnlistmentOptions options)
   {
      var participant = new ParticipantThatRecordsWhatItIsTold(RowsWritten);
      Transaction.Current!.EnlistVolatile(participant, options);
      return participant;
   }

   public class that_commits : A_transaction_that_writes_through_a_connection_from_the_sql_layers_connection_pool
   {
      ///<summary>As Compze's units of work enlist: they prepare their writes during the transaction's prepare, through the transaction's connection.</summary>
      public class with_a_participant_enlisted_with_EnlistDuringPrepareRequired : that_commits
      {
         readonly ParticipantThatRecordsWhatItIsTold _participant;

         public with_a_participant_enlisted_with_EnlistDuringPrepareRequired() =>
            _participant = TransactionScopeCe.Execute(() =>
            {
               var participant = EnlistAParticipant(EnlistmentOptions.EnlistDuringPrepareRequired);
               WriteARow();
               return participant;
            });

         [PCT] public void the_participant_is_told_that_it_committed() => _participant.ToldOutcome.Must().Be(TransactionStatus.Committed);

         [PCT] public void the_participant_is_told_only_once_the_database_has_committed() => _participant.RowsTheDatabaseHeldWhenToldItCommitted.Must().Be(1L);
      }

      public class with_a_participant_enlisted_with_no_options : that_commits
      {
         readonly ParticipantThatRecordsWhatItIsTold _participant;

         public with_a_participant_enlisted_with_no_options() =>
            _participant = TransactionScopeCe.Execute(() =>
            {
               var participant = EnlistAParticipant(EnlistmentOptions.None);
               WriteARow();
               return participant;
            });

         [PCT] public void the_participant_is_told_that_it_committed() => _participant.ToldOutcome.Must().Be(TransactionStatus.Committed);

         [PCT] public void the_participant_is_told_only_once_the_database_has_committed() => _participant.RowsTheDatabaseHeldWhenToldItCommitted.Must().Be(1L);
      }
   }

   ///<summary>A durable resource of the transaction's own, as a consumer's own resource manager would be, takes part in it before the<br/>
   /// connection: the connection can't take part as the transaction's one durable resource.</summary>
   public class that_holds_a_durable_resource_of_its_own : A_transaction_that_writes_through_a_connection_from_the_sql_layers_connection_pool
   {
      readonly Exception _thrownWriting;

      public that_holds_a_durable_resource_of_its_own() =>
         _thrownWriting = Invoking(() => TransactionScopeCe.Execute(() =>
                          {
                             DurableResourceOfItsOwn.TakesPartInTheAmbientTransaction();
                             WriteARow();
                          }))
                         .Must().Throw<Exception>().Which;

      [PCT] public void writing_throws_that_the_transaction_holds_a_durable_resource_already() =>
         _thrownWriting.Must().Satisfy(it => it is DurableResourceAlreadyInTheTransactionException);

      [PCT] public void what_it_throws_holds_the_refusal_by_dotnet_to_make_the_transaction_distributed() =>
         _thrownWriting.InnerException.Must().Satisfy(it => it is NotSupportedException);

      [PCT] public void the_database_holds_no_row_the_transaction_wrote() => RowsWritten().Must().Be(0L);
   }

   ///<summary>The database fails the commit, as a full disk, a lost connection or a deferred constraint can make it: each sql layer is made<br/>
   /// to fail it its own way, by <see cref="MakeTheDatabaseFailTheCommit"/>.</summary>
   public class whose_commit_the_database_fails_with_a_participant_enlisted_with_EnlistDuringPrepareRequired : A_transaction_that_writes_through_a_connection_from_the_sql_layers_connection_pool
   {
      ParticipantThatRecordsWhatItIsTold? _participant;
      readonly Exception _thrownCompletingTheTransaction;
      TransactionStatus? _statusTheTransactionCompletedWith;

      public whose_commit_the_database_fails_with_a_participant_enlisted_with_EnlistDuringPrepareRequired() =>
         _thrownCompletingTheTransaction = Invoking(() => TransactionScopeCe.Execute(() =>
                                           {
                                              Transaction.Current!.TransactionCompleted += (_, completed) => _statusTheTransactionCompletedWith = completed.Transaction!.TransactionInformation.Status;
                                              _participant = EnlistAParticipant(EnlistmentOptions.EnlistDuringPrepareRequired);
                                              WriteARow();
                                              MakeTheDatabaseFailTheCommit();
                                           }))
                                          .Must().Throw<Exception>().Which;

      [PCT] public void completing_the_transaction_throws_that_it_aborted_or_is_in_doubt() =>
         _thrownCompletingTheTransaction.Must().Satisfy(it => it is TransactionAbortedException or TransactionInDoubtException);

      [PCT] public void the_database_holds_no_row_the_transaction_wrote() => RowsWritten().Must().Be(0L);

      [PCT] public void the_participant_is_told_that_the_transaction_aborted_or_is_in_doubt() =>
         _participant!.ToldOutcome.Must().Satisfy(it => it is TransactionStatus.Aborted or TransactionStatus.InDoubt);

      [PCT] public void the_transaction_completes_aborted_or_in_doubt() =>
         _statusTheTransactionCompletedWith.Must().Satisfy(it => it is TransactionStatus.Aborted or TransactionStatus.InDoubt);

      ///<summary>Has the database fail to commit the ambient transaction, which has written through the connection.</summary>
      ///<remarks>SQLite runs a commit hook as it commits, and one that answers non-zero turns the commit into a rollback, and fails it.<br/>
      /// PostgreSQL checks a deferred foreign key as it commits. MySQL and SQL Server lose the connection: another session kills it,<br/>
      /// which rolls its transaction back.</remarks>
      void MakeTheDatabaseFailTheCommit()
      {
         switch(TestEnv.SqlLayer)
         {
            case SqlLayer.Sqlite or SqlLayer.SqliteMemory:
               var connection = _database.Container.Resolve<ISqliteConnectionPool>().UseCommand(command => command.Connection!);
               raw.sqlite3_commit_hook(connection.Handle, _ => 1, null);
               break;
            case SqlLayer.PgSql:
               _database.ExecuteNonQuery("INSERT INTO ChildOfNoParent (ParentId) VALUES (1)");
               break;
            case SqlLayer.MySql:
               KillTheTransactionsSession(_database.ExecuteScalar("SELECT CONNECTION_ID()"));
               break;
            case SqlLayer.MsSql:
               KillTheTransactionsSession(_database.ExecuteScalar("SELECT @@SPID"));
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(SqlLayer), TestEnv.SqlLayer, null);
         }

         void KillTheTransactionsSession(object? sessionId) => TransactionScopeCe.SuppressAmbient(() => _database.ExecuteNonQuery($"KILL {sessionId}"));
      }
   }
}
