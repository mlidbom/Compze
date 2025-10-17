using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
internal static class SqlExceptions
{
   internal static class MySql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 1062;
      internal static bool IsPrimaryKeyViolation(MySqlException e) => e.Data["Server Error Code"] as int? == PrimaryKeyViolationSqlErrorNumber;
   }
}
