using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._internal;
using Compze.Sql.MySql._internal;
using MySqlConnector;

namespace Compze.Sql.MySql._private;

///<summary>The transaction a MySQL connection begins in its database to take part in an ambient transaction, as that transaction's<br/>
/// decision (<see cref="DatabaseTransactionAsTheDecision"/>), at the ambient transaction's isolation level.</summary>
///<remarks>Compze begins it, rather than letting MySqlConnector enlist the connection, which <see cref="CompzeMySqlConnectionString"/><br/>
/// turns off: MySqlConnector's enlistment commits only when it is told the outcome, after the participants told before it.</remarks>
///<remarks>Why not XA, with which MySQL could truly prepare a commit, and take part in a distributed transaction: a prepared XA<br/>
/// transaction outlives a crash of the process, holding its locks on the server until something recovers it, with XA RECOVER and XA<br/>
/// COMMIT or XA ROLLBACK. Compze has no such recovery, and keeps each database in transactions of its own anyway, so a crash between<br/>
/// the two phases would leave locked rows for someone to resolve by hand (weighed 2026-11-24, with Magnus's agreement).</remarks>
sealed class MySqlTransactionAsTheDecision : DatabaseTransactionAsTheDecision
{
   ///<summary>Has <paramref name="databaseTransaction"/>, which <see cref="BeginDatabaseTransaction"/> or <see cref="BeginDatabaseTransactionAsync"/> began, take part in<br/>
   /// <paramref name="ambientTransaction"/> as its decision.</summary>
   internal MySqlTransactionAsTheDecision(MySqlTransaction databaseTransaction, Transaction ambientTransaction)
      : base(databaseTransaction, ambientTransaction, "a MySQL connection") => TakePartInTheAmbientTransaction();

   ///<summary>Begins a transaction on <paramref name="connection"/> to take part in <paramref name="ambientTransaction"/>, at its isolation level.</summary>
   internal static MySqlTransaction BeginDatabaseTransaction(MySqlConnection connection, Transaction ambientTransaction) =>
      connection.BeginTransaction(IsolationLevelOf(ambientTransaction));

   ///<summary>As <see cref="BeginDatabaseTransaction"/>, asynchronously.</summary>
   internal static async Task<MySqlTransaction> BeginDatabaseTransactionAsync(MySqlConnection connection, Transaction ambientTransaction) =>
      await connection.BeginTransactionAsync(IsolationLevelOf(ambientTransaction)).caf();

   ///<summary>An error the server answered the commit with carries a SQL state, and means that it didn't commit. Any other failure, such<br/>
   /// as a connection that broke, which MySqlConnector reports with no SQL state, leaves unknown whether the commit reached the server.</summary>
   protected override bool CertainlyDidNotCommitAfter(Exception commitFailure) => commitFailure is MySqlException { SqlState: not null };
}
