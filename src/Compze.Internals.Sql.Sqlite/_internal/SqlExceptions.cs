using Compze.Internals.SystemCE;
using Microsoft.Data.Sqlite;

namespace Compze.Internals.Sql.Sqlite._internal;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
static class SqlExceptions
{
#pragma warning disable CA1724 // Type name intentionally matches namespace concept
   internal static class Sqlite
   {
      const int PrimaryKeyViolationSqliteErrorCode = 19; // SQLITE_CONSTRAINT
      public static bool IsPrimaryKeyViolation(SqliteException e) => e.SqliteErrorCode == PrimaryKeyViolationSqliteErrorCode && e.Message.ContainsOrdinal("UNIQUE constraint failed");

      const int DatabaseIsLockedSqliteErrorCode = 5;      // SQLITE_BUSY:   another connection holds the lock we need ("database is locked")
      const int DatabaseTableIsLockedSqliteErrorCode = 6; // SQLITE_LOCKED: a connection in the same shared cache holds a conflicting table lock ("database table is locked")

      // SQLite permits only a single writer per database. Under concurrency a connection that cannot immediately
      // acquire the lock it needs fails with one of these codes. Both are transient: the conflicting transaction
      // will release its lock, after which the operation can succeed. Callers should wait and retry the unit of
      // work rather than treat the conflict as a real failure.
      public static bool IsTransientLockConflict(SqliteException e) =>
         e.SqliteErrorCode is DatabaseIsLockedSqliteErrorCode or DatabaseTableIsLockedSqliteErrorCode;
   }
}
