using Compze.Sql.Common;
using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql.Private;

static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}