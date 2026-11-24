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
         //Compze's connection takes part in an ambient transaction itself, as its decision (CompzeMySqlConnection). MySqlConnector's
         //own enlistment would commit only when told the outcome, after the participants told before it, and, by default, as an XA
         //transaction, whose prepare a process dying before the commit leaves on the server, holding its locks until resolved by hand.
         AutoEnlist = false
      }.ConnectionString;
}
