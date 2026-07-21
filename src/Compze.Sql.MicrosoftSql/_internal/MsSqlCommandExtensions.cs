using Compze.Sql.Common._internal;
using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql._internal;

static class MsSqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
      => DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
