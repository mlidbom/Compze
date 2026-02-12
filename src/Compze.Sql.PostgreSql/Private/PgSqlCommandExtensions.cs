using System;
using System.Collections.Generic;
using Compze.Sql.Common;
using Npgsql;

namespace Compze.Sql.PostgreSql.Private;

public static class MyNpgsqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}