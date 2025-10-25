using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.ResourceAccess;
using Document = Compze.Core.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Sql.PostgreSql.DocumentDb;

partial class PgSqlDocumentDbSqlLayer
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
                                                              {Document.ValueTypeId} UUID                        NOT NULL,
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