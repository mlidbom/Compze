using Microsoft.Data.SqlClient;

namespace Compze.Internals.Sql.MicrosoftSql.Internal;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
static class SqlExceptions
{
   internal static class MsSql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 2627;
      public static bool IsPrimaryKeyViolation(SqlException e) => e.Number == PrimaryKeyViolationSqlErrorNumber;
   }
}
