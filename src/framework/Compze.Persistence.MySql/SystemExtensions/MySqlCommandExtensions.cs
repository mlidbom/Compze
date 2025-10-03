using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Compze.Persistence.Common.AdoCE;

namespace Compze.Persistence.MySql.SystemExtensions;

static class MyMySqlCommandExtensions
{
   public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this MySqlCommand @this, Func<MySqlDataReader, T> select) =>
      DbCommandCE.ExecuteReaderAndSelect(@this, select);
}