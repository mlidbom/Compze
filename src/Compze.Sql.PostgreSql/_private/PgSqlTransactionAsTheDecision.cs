using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._internal;
using Compze.Sql.PostgreSql._internal;
using Npgsql;

namespace Compze.Sql.PostgreSql._private;

///<summary>The transaction a PostgreSQL connection begins in its database to take part in an ambient transaction, as that transaction's<br/>
/// decision (<see cref="DatabaseTransactionAsTheDecision"/>), at the ambient transaction's isolation level.</summary>
///<remarks>Compze begins it, rather than letting Npgsql enlist the connection, which <see cref="CompzePgSqlConnectionString"/> turns off:<br/>
/// Npgsql's enlistment is a volatile participant, which commits as the decision only while it is the transaction's one volatile<br/>
/// participant. With another, such as a consumer's own, it prepares a two-phase commit, which a PostgreSQL server refuses by default,<br/>
/// rolling the transaction back, and where the server allows it, commits only when it is told the outcome, after the participants<br/>
/// told before it.</remarks>
sealed class PgSqlTransactionAsTheDecision : DatabaseTransactionAsTheDecision
{
   ///<summary>Has <paramref name="databaseTransaction"/>, which <see cref="BeginDatabaseTransaction"/> or <see cref="BeginDatabaseTransactionAsync"/> began, take part in<br/>
   /// <paramref name="ambientTransaction"/> as its decision.</summary>
   internal PgSqlTransactionAsTheDecision(NpgsqlTransaction databaseTransaction, Transaction ambientTransaction)
      : base(databaseTransaction, ambientTransaction, "a PostgreSQL connection") => TakePartInTheAmbientTransaction();

   ///<summary>Begins a transaction on <paramref name="connection"/> to take part in <paramref name="ambientTransaction"/>, at its isolation level.</summary>
   internal static NpgsqlTransaction BeginDatabaseTransaction(NpgsqlConnection connection, Transaction ambientTransaction) =>
      connection.BeginTransaction(IsolationLevelOf(ambientTransaction));

   ///<summary>As <see cref="BeginDatabaseTransaction"/>, asynchronously.</summary>
   internal static async Task<NpgsqlTransaction> BeginDatabaseTransactionAsync(NpgsqlConnection connection, Transaction ambientTransaction) =>
      await connection.BeginTransactionAsync(IsolationLevelOf(ambientTransaction)).caf();

   ///<summary>An error the server answered the commit with, a <see cref="PostgresException"/>, such as a deferred constraint that the<br/>
   /// commit violated, means that it didn't commit. Any other failure, such as a connection that broke, leaves unknown whether the commit<br/>
   /// reached the server.</summary>
   protected override bool CertainlyDidNotCommitAfter(Exception commitFailure) => commitFailure is PostgresException;
}
