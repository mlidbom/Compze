using System.Threading.Tasks;
using Compze.Sql.MicrosoftSql.Private.DocumentDb;
using Compze.Sql.MicrosoftSql.Private.Tessaging;
using Compze.Sql.MicrosoftSql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Sql.MicrosoftSql.Private;

class MsSqlSqlLayerSchemaManager(IMsSqlConnectionPool connectionPool)
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<MsSqlSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<MsSqlSqlLayerSchemaManager>()
                                         .CreatedBy((IMsSqlConnectionPool connectionPool) => new MsSqlSqlLayerSchemaManager(connectionPool)));
   }

   bool _initialized = false;
   readonly IMsSqlConnectionPool _connectionPool = connectionPool;

   public async Task EnsureSchemaInitializedAsync()
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
         }).caf();
         _initialized = true;
      }
   }

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
