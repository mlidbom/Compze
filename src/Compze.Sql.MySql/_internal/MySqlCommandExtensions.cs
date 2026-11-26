using Compze.Sql.Common._internal;
using MySqlConnector;

namespace Compze.Sql.MySql._internal;

static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);

   public static Task<IReadOnlyList<T>> ExecuteReaderAndSelectAsync<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelectAsync(@this, select);
}