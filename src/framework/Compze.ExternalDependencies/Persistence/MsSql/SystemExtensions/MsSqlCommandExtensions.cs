using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Compze.Persistence.Common.AdoCE;

namespace Compze.Persistence.MsSql.SystemExtensions;

static class MsSqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
      => DbCommandCE.ExecuteReaderAndSelect(@this, select);
}