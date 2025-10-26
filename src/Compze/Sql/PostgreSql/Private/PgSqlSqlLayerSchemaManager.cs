using System.Threading.Tasks;
using Compze.Sql.PostgreSql.Private.DocumentDb;
using Compze.Sql.PostgreSql.Private.Tessaging;
using Compze.Sql.PostgreSql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Sql.PostgreSql.Private;

class PgSqlSqlLayerSchemaManager(IPgSqlConnectionPool connectionPool)
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<PgSqlSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<PgSqlSqlLayerSchemaManager>()
                                         .CreatedBy((IPgSqlConnectionPool connectionPool) => new PgSqlSqlLayerSchemaManager(connectionPool)));
   }

   bool _initialized = false;
   readonly IPgSqlConnectionPool _connectionPool = connectionPool;

   public async Task EnsureSchemaInitializedAsync()
   {
      if(!_initialized)
      {
         _initialized = true;
         await TransactionScopeCe.SuppressAmbientAsync(async () =>
         {
            //todo: test if running them in parallel is faster
            await _connectionPool.ExecuteNonQueryAsync($"""

                                                        {PgSqlDocumentDbSqlLayer.SchemaCreationSql}

                                                        {PgSqlInboxSqlLayer.SchemaCreationSql}

                                                        {PgSqlOutboxSqlLayer.SchemaCreationSql}

                                                        {PgSqlTeventStoreSqlLayer.SchemaCreationSql}

                                                        """).caf();
         }).caf();
      }
   }

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
