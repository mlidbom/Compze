using System.Collections.Generic;

namespace Compze.Utilities.Testing.DbPool;

public interface IDbPoolSqlLayer
{
   void ResetDatabase(DbPoolDatabase db);

   string ConnectionStringFor(DbPoolDatabase db);

   void Dispose(IReadOnlyList<DbPoolDatabase> reservedDatabases)
   {
   }

   void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db);
}
