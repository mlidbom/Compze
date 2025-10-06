using Compze.Persistence.Common;
using Microsoft.Data.SqlClient;

namespace Compze.Persistence.MicrosoftSql.Infrastructure;

public static class MsSqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
      => DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
