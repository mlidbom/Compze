using Compze.Utilities.SystemCE;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Private;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
internal static class SqlExceptions
{
#pragma warning disable CA1724 // Type name intentionally matches namespace concept
   internal static class Sqlite
   {
      const int PrimaryKeyViolationSqliteErrorCode = 19; // SQLITE_CONSTRAINT
      public static bool IsPrimaryKeyViolation(SqliteException e) => e.SqliteErrorCode == PrimaryKeyViolationSqliteErrorCode && e.Message.ContainsCE("UNIQUE constraint failed");
   }
}
