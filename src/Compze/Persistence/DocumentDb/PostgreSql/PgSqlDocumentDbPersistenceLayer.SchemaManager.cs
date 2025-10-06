using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.TransactionsCE;
using Document = Compze.Abstractions.Internal.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Compze.Persistence.DocumentDb.PostgreSql;

partial class PgSqlDocumentDbPersistenceLayer
{
   class SchemaManager(IPgSqlConnectionPool connectionPool)
   {
      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
      bool _initialized = false;
      readonly IPgSqlConnectionPool _connectionPool = connectionPool;

      internal void EnsureInitialized() => _monitor.Update(() =>
      {
         if(!_initialized)
         {
            TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
            {
               _connectionPool.PrepareAndExecuteNonQuery($"""

                                                          CREATE TABLE IF NOT EXISTS {Document.TableName} 
                                                          (
                                                              {Document.Id}          VARCHAR(500)                NOT NULL,
                                                              {Document.ValueTypeId} CHAR(38)                    NOT NULL,
                                                              {Document.Created}     TIMESTAMP with time zone    NOT NULL,
                                                              {Document.Updated}     TIMESTAMP with time zone    NOT NULL,
                                                              {Document.Value}       TEXT                        NOT NULL,
                                                          
                                                              PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
                                                          )


                                                          """);
            });
         }

         _initialized = true;
      });
   }
}