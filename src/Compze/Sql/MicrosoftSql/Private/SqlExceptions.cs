using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql.Private;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
static class SqlExceptions
{
   public static class MsSql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 2627;
      public static bool IsPrimaryKeyViolation(SqlException e) => e.Number == PrimaryKeyViolationSqlErrorNumber;
   }
}
