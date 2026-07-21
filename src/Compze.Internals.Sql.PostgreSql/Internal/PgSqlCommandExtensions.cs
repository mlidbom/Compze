using Compze.Internals.Sql.Common;
using Npgsql;

namespace Compze.Internals.Sql.PostgreSql.Internal;

static class MyNpgsqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}