using Compze.Internals.Sql.Common;
using Microsoft.Data.SqlClient;

namespace Compze.Internals.Sql.MicrosoftSql.Private;

static class MsSqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
      => DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
