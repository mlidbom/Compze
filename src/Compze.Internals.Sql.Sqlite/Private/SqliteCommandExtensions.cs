using Compze.Internals.Sql.Common;
using Microsoft.Data.Sqlite;

namespace Compze.Internals.Sql.Sqlite.Private;

static class SqliteCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqliteCommand @this, Func<SqliteDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
