using Compze.Persistence.Common.AdoCE;
using Npgsql;

namespace Compze.Persistence.PostgreSql.SystemExtensions;

static class MyNpgsqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}