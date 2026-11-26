using System.Globalization;
using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Hosting.Testing;
using Compze.Threading.Testing;
using Compze.xUnitMatrix;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Sql.InternalSpecifications.TransactionCommit;

///<summary>A transaction that takes the connection another transaction returned to SqlClient's pool as that transaction completed: the<br/>
/// sql layer's connection pool returns a transaction's connection when the transaction completes, and another transaction may take<br/>
/// the same physical connection from SqlClient's pool at once.</summary>
///<remarks>SqlClient handles a transaction's completion on the connection in a handler of its own, and in SqlClient 6.1.3 that handler<br/>
/// dooms the connection if the connection takes part in a transaction by then, whichever transaction that is. When the pool returned<br/>
/// the connection before SqlClient's handler had run, the next transaction on the same physical connection failed to commit: "The<br/>
/// requested operation cannot be completed because the connection has been broken." SqlClient's diagnostic events start the second<br/>
/// transaction at that moment: inside the first transaction's close of its connection, once the physical connection is back in the pool.</remarks>
public class A_transaction_on_the_connection_another_transactions_completion_returned_to_the_pool : UniversalTestBase
{
   const string DomainDatabaseName = "ConnectionReturnedAsATransactionCompletes";
   const string TheFirstTransactionsCommand = "-- the first transaction's command";
   const string SqlClientsEventsStartTheSecondTransaction = "SqlClient's diagnostic events are what start the second transaction at that moment";

   readonly DomainDatabaseOnTheCurrentSqlLayer _database = DomainDatabaseOnTheCurrentSqlLayer.Named(DomainDatabaseName);
   object? _firstTransactionsSession;
   object? _secondTransactionsSession;
   Exception? _secondTransactionsFailure;

   protected override async Task InitializeAsyncInternal()
   {
      _database.ExecuteNonQuery("CREATE TABLE Written (Id INT NOT NULL PRIMARY KEY)");
      var theSecondTransactionHasItsConnection = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "theSecondTransactionHasItsConnection");
      var theFirstTransactionHasCompleted = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "theFirstTransactionHasCompleted");
      var secondTransaction = Task.CompletedTask;

      using(new WhenSqlClientHasClosedTheConnectionThatRan(TheFirstTransactionsCommand, () =>
            {
               secondTransaction = TaskCE.RunOnDedicatedThreadAsync(() => TransactionScopeCe.Execute(() =>
               {
                  _secondTransactionsSession = _database.ExecuteScalar("SELECT @@SPID");
                  theSecondTransactionHasItsConnection.Open();
                  theFirstTransactionHasCompleted.AwaitPassThrough();
                  _database.ExecuteNonQuery("INSERT INTO Written (Id) VALUES (1)");
               }, TransactionScopeOption.RequiresNew));
               theSecondTransactionHasItsConnection.AwaitPassThrough();
            }))
      {
         _firstTransactionsSession = TransactionScopeCe.Execute(() => _database.ExecuteScalar($"SELECT @@SPID {TheFirstTransactionsCommand}"));
         theFirstTransactionHasCompleted.Open();
         try
         {
            await secondTransaction;
         }
         catch(Exception failure)
         {
            _secondTransactionsFailure = failure;
         }
      }
   }

   protected override async Task DisposeAsyncInternal() => await _database.DisposeAsync();

   long RowsWritten() => Convert.ToInt64(TransactionScopeCe.Execute(() => _database.ExecuteScalar("SELECT COUNT(*) FROM Written"), TransactionScopeOption.Suppress), CultureInfo.InvariantCulture);

   [PCT, Skip<SqlLayer>([SqlLayer.MySql, SqlLayer.PgSql, SqlLayer.Sqlite, SqlLayer.SqliteMemory], SqlClientsEventsStartTheSecondTransaction)]
   public void it_runs_on_the_same_session_as_the_other_transaction() => _secondTransactionsSession.Must().Be(_firstTransactionsSession);

   [PCT, Skip<SqlLayer>([SqlLayer.MySql, SqlLayer.PgSql, SqlLayer.Sqlite, SqlLayer.SqliteMemory], SqlClientsEventsStartTheSecondTransaction)]
   public void it_commits() => _secondTransactionsFailure.Must().BeNull();

   [PCT, Skip<SqlLayer>([SqlLayer.MySql, SqlLayer.PgSql, SqlLayer.Sqlite, SqlLayer.SqliteMemory], SqlClientsEventsStartTheSecondTransaction)]
   public void what_it_wrote_is_in_the_database() => RowsWritten().Must().Be(1L);
}
