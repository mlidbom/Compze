using System;
using System.Collections.Generic;
using Compze.Sql.Common;
using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql.Private;

internal static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}