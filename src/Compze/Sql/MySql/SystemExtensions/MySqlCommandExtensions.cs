using Compze.Sql.Common;
using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql.Infrastructure.SystemExtensions;

internal static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}