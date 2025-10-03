using Compze.Persistence.Common.AdoCE;
using Microsoft.Data.SqlClient;

namespace Compze.Persistence.MicrosoftSqlServer.SystemExtensions;

static class MsSqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
      => DbCommandCE.ExecuteReaderAndSelect(@this, select);
}