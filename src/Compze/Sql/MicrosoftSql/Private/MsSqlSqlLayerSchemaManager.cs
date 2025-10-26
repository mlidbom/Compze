using System.Threading.Tasks;
using Compze.Sql.MicrosoftSql.Private.DocumentDb;
using Compze.Sql.MicrosoftSql.Private.Tessaging;
using Compze.Sql.MicrosoftSql.Private.TEventStore;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.ResourceAccess;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Sql.MicrosoftSql.Private;

class MsSqlSqlLayerSchemaManager(IMsSqlConnectionPool connectionPool)
{
   readonly MonitorCE _monitor = MonitorCE.WithTimeout(5.Seconds());
   bool _initialized = false;
   readonly IMsSqlConnectionPool _connectionPool = connectionPool;

   public async Task EnsureTablesExistAsync()
   {
      if(!_initialized)
      {
         using(_monitor.TakeUpdateLock())
         {
            if(!_initialized)
            {
               await TransactionScopeCe.SuppressAmbientAsync(async () =>
               {
                  await _connectionPool.ExecuteNonQueryAsync($"""

                                                              {MsSqlDocumentDbSqlLayer.SchemaCreationSql}

                                                              {MsSqlInboxSqlLayer.SchemaCreationSql}

                                                              {MsSqlOutboxSqlLayer.SchemaCreationSql}

                                                              {MsSqlTeventStoreSqlLayer.SchemaCreationSql}

                                                              """).caf();
               });
               _initialized = true;
            }
         }
      }
   }

   public void EnsureTablesExist() => EnsureTablesExistAsync().Wait();
}
