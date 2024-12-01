using System;
using System.Collections.Generic;
using Npgsql;
using Compze.Persistence.Common.AdoCE;

namespace Compze.Persistence.PgSql.SystemExtensions;

static class MyNpgsqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}