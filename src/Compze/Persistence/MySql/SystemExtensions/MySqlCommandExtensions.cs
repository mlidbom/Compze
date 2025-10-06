using MySql.Data.MySqlClient;
using Compze.Persistence.Common.AdoCE;

namespace Compze.Persistence.MySql.Infrastructure;

internal static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}