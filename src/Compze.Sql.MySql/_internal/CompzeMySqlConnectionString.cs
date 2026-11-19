using MySqlConnector;

namespace Compze.Sql.MySql._internal;

///<summary>The connection string every Compze connection to MySQL opens with: the configured one, with the settings Compze<br/>
/// relies on applied.</summary>
///<remarks>MySqlConnector keys its connection pools by connection string, so whatever clears the pool Compze's connections<br/>
/// use, as the <c>DbPool</c> does with <see cref="MySqlConnection.ClearPool"/> before it drops a database, must name the<br/>
/// pool by this string too.</remarks>
static class CompzeMySqlConnectionString
{
   internal static string From(string configuredConnectionString) =>
      new MySqlConnectionStringBuilder(configuredConnectionString)
      {
         //Compze never uses distributed transactions: TransactionCE.NoTransactionEscalationScope asserts against escalation.
         //MySqlConnector's default, true, makes every ambient transaction an XA transaction, which costs an XA PREPARE before
         //each commit, and a process dying between the prepare and the commit leaves a prepared transaction on the server,
         //holding its locks until it is resolved by hand. False makes it a plain local transaction, committed at Commit, at the
         //isolation level the transaction asks for: the XA path ignores that and runs at the server's default.
         UseXaTransactions = false
      }.ConnectionString;
}
