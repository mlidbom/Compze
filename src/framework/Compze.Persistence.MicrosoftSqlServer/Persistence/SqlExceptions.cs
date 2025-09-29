using Compze.SystemCE;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Compze.Persistence;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
static class SqlExceptions
{
   internal static class MsSql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 2627;
      internal static bool IsPrimaryKeyViolation(SqlException e) => e.Number == PrimaryKeyViolationSqlErrorNumber;
   }
}
