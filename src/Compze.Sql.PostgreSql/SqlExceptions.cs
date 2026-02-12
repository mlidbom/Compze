using Compze.Utilities.SystemCE;
using Npgsql;

namespace Compze.Sql.PostgreSql;

//We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
static class SqlExceptions
{
   internal static class PgSql
   {
      const int PrimaryKeyViolationSqlErrorNumber = 23505;
      internal static bool IsPrimaryKeyViolation(PostgresException e) => e.SqlState == PrimaryKeyViolationSqlErrorNumber.ToStringInvariant();
   }
}