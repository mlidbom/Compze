using System;
using System.Collections.Generic;
using Compze.Sql.Common;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Private;

internal static class SqliteCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqliteCommand @this, Func<SqliteDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
