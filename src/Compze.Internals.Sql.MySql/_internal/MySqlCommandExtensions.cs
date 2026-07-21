using Compze.Internals.Sql.Common;
using MySqlConnector;

namespace Compze.Internals.Sql.MySql._internal;

static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}