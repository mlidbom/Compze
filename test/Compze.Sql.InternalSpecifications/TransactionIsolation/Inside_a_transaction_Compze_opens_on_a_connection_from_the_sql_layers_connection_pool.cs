using System.Data;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Sql.MySql._internal;
using Compze.Sql.PostgreSql._internal;
using Compze.Sql.Sqlite._internal;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitMatrix;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Sql.InternalSpecifications.TransactionIsolation;

///<summary>Asks each sql layer's server what isolation level it runs Compze's transactions at. Every transaction Compze opens,<br/>
/// through <see cref="TransactionScopeCe"/>, asks for read committed, and the connection the sql layer's connection pool gives out<br/>
/// inside it must run it at that level: SQL Server, PostgreSQL and MySQL each report read committed for it.</summary>
///<remarks>A provider can run the transaction at another level without anything else failing, and a wrong isolation level breaks<br/>
/// things in ways hard to trace to it: MySqlConnector's XA transactions, which Compze used until <c>CompzeMySqlConnectionString</c><br/>
/// turned them off, ran at the server's default, repeatable read. Compze.Tests.Integration's<br/>
/// <c>While_a_transaction_that_has_read_a_document_is_open</c> specifies the same through the public API, by what the transaction reads.</remarks>
///<remarks>Each server is asked in its own words: SQL Server for its session's <c>transaction_isolation_level</c> in<br/>
/// <c>sys.dm_exec_sessions</c>, PostgreSQL for its <c>transaction_isolation</c> setting, and MySQL for the isolation level of the<br/>
/// transaction <c>performance_schema.events_transactions_current</c> has active on the connection's thread, which is the current<br/>
/// transaction's own level, where <c>@@transaction_isolation</c> is only the session's default.<br/>
/// SQLite has no read committed. Microsoft.Data.Sqlite begins a transaction asked for read committed as a serializable one, with<br/>
/// <c>BEGIN IMMEDIATE</c>, which takes the database's write lock at once, and leaves <c>PRAGMA read_uncommitted</c> at its default, off.</remarks>
public class Inside_a_transaction_Compze_opens_on_a_connection_from_the_sql_layers_connection_pool : UniversalTestBase
{
   const string DomainDatabaseName = "IsolationLevelDatabase";
   const string ReadCommitted = "READ COMMITTED";

   const string SqliteHasNoReadCommitted = "SQLite has no read committed: the specifications on sqlite say what it runs instead.";
   const string TheServersReportTheirIsolationLevel = "The servers report their isolation level, which the specification asking the server specifies.";

   ///<summary>Asks SQL Server for its session's <c>transaction_isolation_level</c>, in the words the other servers use.</summary>
   const string MsSqlTheSessionsIsolationLevel = """
                                                 SELECT CASE transaction_isolation_level
                                                           WHEN 1 THEN 'READ UNCOMMITTED'
                                                           WHEN 2 THEN 'READ COMMITTED'
                                                           WHEN 3 THEN 'REPEATABLE READ'
                                                           WHEN 4 THEN 'SERIALIZABLE'
                                                           WHEN 5 THEN 'SNAPSHOT'
                                                           ELSE 'UNSPECIFIED'
                                                        END
                                                 FROM sys.dm_exec_sessions
                                                 WHERE session_id = @@SPID
                                                 """;

   ///<summary>Asks MySQL for the isolation level of the transaction active on the connection's thread.</summary>
   const string MySqlTheActiveTransactionsIsolationLevel = """
                                                           SELECT ISOLATION_LEVEL
                                                           FROM performance_schema.events_transactions_current
                                                           WHERE THREAD_ID = PS_CURRENT_THREAD_ID() AND STATE = 'ACTIVE'
                                                           """;

   ///<summary>Asks PostgreSQL for the transaction's <c>transaction_isolation</c> setting, as <c>SHOW transaction_isolation</c> would, in<br/>
   /// the words the other servers use.</summary>
   const string PgSqlTheTransactionsIsolationLevel = "SELECT UPPER(current_setting('transaction_isolation'))";

   readonly DomainDatabaseOnTheCurrentSqlLayer _database = DomainDatabaseOnTheCurrentSqlLayer.Named(DomainDatabaseName);

   protected override async Task DisposeAsyncInternal() => await _database.DisposeAsync();

   [PCT, Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], SqliteHasNoReadCommitted)]
   public void the_server_reports_that_it_runs_the_transaction_at_read_committed() =>
      TheIsolationLevelTheServerReportsForTheTransaction().Must().Be(ReadCommitted);

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], TheServersReportTheirIsolationLevel)]
   public void on_sqlite_which_has_no_read_committed_the_transaction_Microsoft_Data_Sqlite_begins_is_serializable() =>
      TheIsolationLevelOfTheSqliteTransaction().Must().Be(IsolationLevel.Serializable);

   [PCT, Skip<SqlLayer>([SqlLayer.MsSql, SqlLayer.MySql, SqlLayer.PgSql], TheServersReportTheirIsolationLevel)]
   public void on_sqlite_the_connection_does_not_read_uncommitted_changes() =>
      TheSqliteConnectionsReadUncommittedPragma().Must().Be(0L);

   string? TheIsolationLevelTheServerReportsForTheTransaction() =>
      TransactionScopeCe.Execute(() => (string?)(TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql => _database.Container.Resolve<IMsSqlConnectionPool>().ExecuteScalar(MsSqlTheSessionsIsolationLevel),
         SqlLayer.MySql => _database.Container.Resolve<IMySqlConnectionPool>().ExecuteScalar(MySqlTheActiveTransactionsIsolationLevel),
         SqlLayer.PgSql => _database.Container.Resolve<IPgSqlConnectionPool>().ExecuteScalar(PgSqlTheTransactionsIsolationLevel),
         _ => throw new ArgumentOutOfRangeException(nameof(SqlLayer), TestEnv.SqlLayer, SqliteHasNoReadCommitted)
      }));

   ///<summary>The isolation level Microsoft.Data.Sqlite reports for the transaction Compze's SQLite connection began: the connection<br/>
   /// hands the transaction to every command it creates.</summary>
   IsolationLevel? TheIsolationLevelOfTheSqliteTransaction() =>
      TransactionScopeCe.Execute(() => _database.Container.Resolve<ISqliteConnectionPool>().UseCommand(command => command.Transaction?.IsolationLevel));

   long? TheSqliteConnectionsReadUncommittedPragma() =>
      TransactionScopeCe.Execute(() => (long?)_database.Container.Resolve<ISqliteConnectionPool>().ExecuteScalar("PRAGMA read_uncommitted"));
}
