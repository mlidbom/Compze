using System;
using System.Collections.Generic;
using Compze.Sql.Common;
using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql.Private;

static class MsSqlCommandExtensions
{
   internal static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
      => DbCommandCE.ExecuteReaderAndSelect(@this, select);
}
