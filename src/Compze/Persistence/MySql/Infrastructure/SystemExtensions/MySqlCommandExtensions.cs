using Compze.Persistence.Common;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql.Infrastructure.SystemExtensions;

internal static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}