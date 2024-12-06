using Compze.Persistence.MySql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Compze.SystemCE.TransactionsCE;
using Document = Compze.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Compze.Persistence.MySql.DocumentDb;

partial class MySqlDocumentDbPersistenceLayer
{
   class SchemaManager(IMySqlConnectionPool connectionPool)
   {
      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
      bool _initialized = false;
      readonly IMySqlConnectionPool _connectionPool = connectionPool;

      internal void EnsureInitialized() => _monitor.Update(() =>
      {
         if(!_initialized)
         {
            TransactionScopeCe.SuppressAmbientAndExecuteInNewTransaction(() =>
            {
               _connectionPool.ExecuteNonQuery($"""

                                                CREATE TABLE IF NOT EXISTS {Document.TableName} 
                                                (
                                                  {Document.Id}          VARCHAR(500) NOT NULL,
                                                  {Document.ValueTypeId} CHAR(38)     NOT NULL,
                                                  {Document.Created}     DATETIME     NOT NULL,
                                                  {Document.Updated}     DATETIME     NOT NULL,
                                                  {Document.Value}       MEDIUMTEXT   NOT NULL,
                                                
                                                  PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
                                                )
                                                ENGINE = InnoDB
                                                DEFAULT CHARACTER SET = utf8mb4;


                                                """);
            });
         }

         _initialized = true;
      });
   }
}