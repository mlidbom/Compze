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

   internal static class MySql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 1062;
      internal static bool IsPrimaryKeyViolation(MySqlException e) => e.Data["Server Error Code"] as int? == PrimaryKeyViolationSqlErrorNumber;
   }

   internal static class PgSql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 23505;
      internal static bool IsPrimaryKeyViolation(PostgresException e) => e.SqlState == PrimaryKeyViolationSqlErrorNumber.ToStringInvariant();
   }
}
