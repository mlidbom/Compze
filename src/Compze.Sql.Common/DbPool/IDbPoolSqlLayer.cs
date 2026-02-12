using System.Collections.Generic;

namespace Compze.Sql.Common.DbPool;

public interface IDbPoolSqlLayer
{
   void ResetDatabase(DbPoolDatabase db);

   string ConnectionStringFor(DbPoolDatabase db);

   void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
   }

   void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db);
}
