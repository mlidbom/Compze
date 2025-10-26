using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.ResourceAccess;
using Document = Compze.Core.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Sql.MicrosoftSql.Private.DocumentDb;

partial class MsSqlDocumentDbSqlLayer
{
   internal const string SchemaCreationSql =
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{Document.TableName}')
       BEGIN 
           CREATE TABLE {Document.TableName}
           (
               {Document.Id}          nvarchar(500)    NOT NULL,
               {Document.ValueTypeId} uniqueidentifier NOT NULL,
               {Document.Created}     datetime2        NOT NULL,
               {Document.Updated}     datetime2        NOT NULL,
               {Document.Value}       nvarchar(max)    NOT NULL,
                  
               CONSTRAINT PK_{Document.TableName} PRIMARY KEY CLUSTERED 
                  ({Document.Id} ASC, {Document.ValueTypeId} ASC)
                  WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF)
           )

       END

       """;

   class SchemaManager(IMsSqlConnectionPool connectionPool)
   {
      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
      bool _initialized = false;
      readonly IMsSqlConnectionPool _connectionPool = connectionPool;

      internal void EnsureInitialized() => _monitor.Update(() =>
      {
         if(!_initialized)
         {
            TransactionScopeCe.SuppressAmbient(() =>
            {
               _connectionPool.ExecuteNonQuery(SchemaCreationSql);
            });
         }

         _initialized = true;
      });
   }
}
