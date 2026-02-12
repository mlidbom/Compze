using System.Collections.Generic;

namespace Compze.Sql.Common.DbPool;

interface IDbPoolSqlLayer
{
   void ResetDatabase(DbPoolDatabase db);

   string ConnectionStringFor(DbPoolDatabase db);

   void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
   }

   void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db);
}
