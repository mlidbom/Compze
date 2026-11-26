using Compze.Sql.Common._internal;
using Npgsql;

namespace Compze.Sql.PostgreSql._internal;

static class MyNpgsqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);

   public static Task<IReadOnlyList<T>> ExecuteReaderAndSelectAsync<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelectAsync(@this, select);

   public static Task<IReadOnlyList<T>> ExecutePreparedReaderAndSelectAsync<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
      DbCommandCE.ExecutePreparedReaderAndSelectAsync(@this, select);
}