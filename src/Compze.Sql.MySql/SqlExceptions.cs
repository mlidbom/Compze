using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
public static class SqlExceptions
{
   public static class MySql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 1062;
      public static bool IsPrimaryKeyViolation(MySqlException e) => e.Data["Server Error Code"] as int? == PrimaryKeyViolationSqlErrorNumber;
   }
}
