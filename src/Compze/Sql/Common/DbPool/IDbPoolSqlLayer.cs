using System.Collections.Generic;

namespace Compze.Utilities.Testing.DbPool;

interface IDbPoolSqlLayer
{
   void ResetDatabase(DbPoolDatabase db);

   string ConnectionStringFor(DbPoolDatabase db);

   void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
   }

   void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db);
}
