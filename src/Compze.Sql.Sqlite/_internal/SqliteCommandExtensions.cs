using Compze.Sql.Common._internal;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite._internal;

static class SqliteCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqliteCommand @this, Func<SqliteDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
