using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
static class SqlExceptions
{
#pragma warning disable CA1724 // Type name intentionally matches namespace concept
   internal static class MySql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 1062;
      public static bool IsPrimaryKeyViolation(MySqlException e) => e.Data["Server Error Code"] as int? == PrimaryKeyViolationSqlErrorNumber;
   }
}
