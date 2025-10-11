using Compze.Sql.Sqlite.Infrastructure;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.ResourceAccess;
using Document = Compze.Abstractions.Internal.Sql.DocumentDb.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Sql.DocumentDb.Sqlite;

partial class SqliteDocumentDbSqlLayer
{
   class SchemaManager(ISqliteConnectionPool connectionPool)
   {
      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
      bool _initialized = false;
      readonly ISqliteConnectionPool _connectionPool = connectionPool;

      internal void EnsureInitialized() => _monitor.Update(() =>
      {
         if(!_initialized)
         {
            TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
            {
               _connectionPool.ExecuteNonQuery($"""

                                                CREATE TABLE IF NOT EXISTS {Document.TableName} 
                                                (
                                                    {Document.Id}          TEXT NOT NULL,
                                                    {Document.ValueTypeId} TEXT NOT NULL,
                                                    {Document.Created}     TEXT NOT NULL,
                                                    {Document.Updated}     TEXT NOT NULL,
                                                    {Document.Value}       TEXT NOT NULL,
                                                       
                                                    PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
                                                )

                                                """);
            });
         }

         _initialized = true;
      });
   }
}
