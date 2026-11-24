using Npgsql;

namespace Compze.Sql.PostgreSql._internal;

///<summary>The connection string every Compze connection to PostgreSQL opens with: the configured one, with the settings Compze<br/>
/// relies on applied.</summary>
///<remarks>Npgsql keys its connection pools by connection string, so whatever clears the pool Compze's connections use, as the<br/>
/// <c>DbPool</c> does with <see cref="NpgsqlConnection.ClearPool"/> before it resets a database, must name the pool by this string too.</remarks>
static class CompzePgSqlConnectionString
{
   internal static string From(string configuredConnectionString) =>
      new NpgsqlConnectionStringBuilder(configuredConnectionString)
      {
         //Compze's connection takes part in an ambient transaction itself, as the transaction's decision: see PgSqlTransactionAsTheDecision.
         //Npgsql's own enlistment commits as the decision only while it is the transaction's one volatile participant: with another, it
         //prepares a two-phase commit, PREPARE TRANSACTION, which a server refuses unless its max_prepared_transactions allows it.
         Enlist = false
      }.ConnectionString;
}
