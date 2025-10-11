using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Infrastructure;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
internal static class SqlExceptions
{
   internal static class Sqlite
   {
      const int PrimaryKeyViolationSqliteErrorCode = 19; // SQLITE_CONSTRAINT
      internal static bool IsPrimaryKeyViolation(SqliteException e) => e.SqliteErrorCode == PrimaryKeyViolationSqliteErrorCode && e.Message.Contains("UNIQUE constraint failed");
   }
}
